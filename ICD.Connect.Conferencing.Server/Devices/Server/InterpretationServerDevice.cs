using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Server.Conferences;
using ICD.Connect.Conferencing.Server.Devices.Client;
using ICD.Connect.Conferencing.Server.Devices.Simpl;
using ICD.Connect.Devices.CrestronSPlus.Devices.SPlus;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Settings;
using ICD.Connect.Settings.CrestronSPlus.SPlusShims.EventArguments;

namespace ICD.Connect.Conferencing.Server.Devices.Server
{
	[PublicAPI]
	public sealed class InterpretationServerDevice : AbstractSPlusDevice<InterpretationServerDeviceSettings>, IInterpretationServerDevice
	{
		public event EventHandler<InterpretationStateEventArgs> OnInterpretationStateChanged;
		public event EventHandler<InterpretationRoomInfoArgs> OnRoomAdded;

		#region RPC Constants

		public const string REGISTER_ROOM_RPC = "RegisterRoom";
		public const string UNREGISTER_ROOM_RPC = "UnregisterRoom";

		public const string DIAL_RPC = "Dial";
		public const string DIAL_TYPE_RPC = "DialType";
		public const string PRIVACY_MUTE_RPC = "PrivacyMute";
		public const string AUTO_ANSWER_RPC = "AutoAnswer";
		public const string DO_NOT_DISTURB_RPC = "DoNotDisturb";

		public const string HOLD_ENABLE_RPC = "HoldEnable";
		public const string HOLD_RESUME_RPC = "HoldResume";
		public const string SEND_DTMF_RPC = "SendDTMF";
		public const string LEAVE_CONFERENCE_RPC = "LeaveConference";
		public const string END_CONFERENCE_RPC = "EndConference";
		public const string START_RECORDING_RPC = "StartRecording";
		public const string STOP_RECORDING_RPC = "StopRecording";
		public const string PAUSE_RECORDING_RPC = "PauseRecording";

		#endregion

		#region Private Members

		private readonly IcdTcpServer m_Server;
		private readonly ServerSerialRpcController m_RpcController;

		private readonly SafeCriticalSection m_SafeCriticalSection;

		// key is interpretation device, value is booth id for that device.
		private readonly Dictionary<ISimplInterpretationDevice, ushort> m_AdapterToBooth;

		// key is guid id of conference, value is the conference
		private readonly Dictionary<Guid, IConference> m_Conferences;

		// Track participants per conference to get updates.
		private readonly Dictionary<IConference, IcdHashSet<IParticipant>> m_ConferencesToParticipants;

		// key is room id, value is booth number
		private readonly Dictionary<int, ushort> m_RoomToBooth;

		// key is tcp client id, value is room id
		private readonly Dictionary<uint, int> m_ClientToRoom;

		// key is room id, value is name
		private readonly Dictionary<int, string> m_RoomToRoomInfo;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public InterpretationServerDevice()
		{
			m_RpcController = new ServerSerialRpcController(this);
			m_AdapterToBooth = new Dictionary<ISimplInterpretationDevice, ushort>();
			m_Conferences = new Dictionary<Guid, IConference>();
			m_ConferencesToParticipants = new Dictionary<IConference, IcdHashSet<IParticipant>>();
			m_RoomToBooth = new Dictionary<int, ushort>();
			m_ClientToRoom = new Dictionary<uint, int>();
			m_RoomToRoomInfo = new Dictionary<int, string>();

			m_SafeCriticalSection = new SafeCriticalSection();

			m_Server = new IcdTcpServer();
			m_RpcController.SetServer(m_Server);
			Subscribe(m_Server);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			Unsubscribe(m_Server);
			m_Server.Stop();

			base.DisposeFinal(disposing);

			ClearAdapters();
			ClearSources();
		}

		#region Public Methods

		/// <summary>
		/// Gets the rooms which are registered with the core, 
		/// but do not currently have interpretation active.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<int> GetAvailableRoomIds()
		{
			m_SafeCriticalSection.Enter();
			try
			{
				return m_ClientToRoom.Where(kvp => !m_RoomToBooth.ContainsKey(kvp.Value))
									 .Select(kvp => kvp.Value)
									 .ToArray();
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		/// <summary>
		/// Gets the booths that are not currently interpreting for any rooms.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<ushort> GetAvailableBoothIds()
		{
			m_SafeCriticalSection.Enter();
			try
			{
				IEnumerable<ushort> usedBooths = m_RoomToBooth.Values;
				return m_AdapterToBooth.Values.Except(usedBooths);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		/// <summary>
		/// Begins forwarding 
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="boothId"></param>
		[PublicAPI]
		public void BeginInterpretation(int roomId, ushort boothId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				if (!m_AdapterToBooth.ContainsValue(boothId))
					return;

				// No change
				if (m_RoomToBooth.ContainsKey(roomId) && m_RoomToBooth[roomId] == boothId)
					return;

				m_RoomToBooth[roomId] = boothId;

				TransmitInterpretationState(boothId);

				OnInterpretationStateChanged.Raise(this, new InterpretationStateEventArgs(roomId, boothId, true));
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		[PublicAPI]
		public void EndInterpretation(int roomId, ushort boothId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				if (!m_AdapterToBooth.ContainsValue(boothId))
					return;

				if (!m_RoomToBooth.ContainsKey(roomId))
					return;

				// booth is not attached to the given room
				if (m_RoomToBooth[roomId] != boothId)
					return;

				uint clientId;
				if (!GetClientIdForAdapter(m_AdapterToBooth.GetKey(boothId), out clientId))
					return;

				m_RoomToBooth.Remove(roomId);

				m_RpcController.CallMethod(clientId, InterpretationClientDevice.SET_INTERPRETATION_STATE_RPC, false);

				OnInterpretationStateChanged.Raise(this, new InterpretationStateEventArgs(roomId, boothId, false));
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		/// <summary>
		/// Gets the Room Name for a given Room Id
		/// </summary>
		/// <param name="roomId"></param>
		/// <returns></returns>
		public string GetRoomName(int roomId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				return m_RoomToRoomInfo.ContainsKey(roomId) ? m_RoomToRoomInfo[roomId] : string.Empty;
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		/// <summary>
		/// Gets the Booth Id for a given Room Id
		/// </summary>
		/// <param name="roomId"></param>
		/// <returns></returns>
		public ushort GetBoothId(int roomId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				return m_RoomToBooth.ContainsKey(roomId) ? m_RoomToBooth[roomId] : (ushort)0;
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		/// <summary>
		/// Gets if the room exists
		/// </summary>
		/// <param name="roomId"></param>
		/// <returns></returns>
		public ushort GetRoomExists(int roomId)
		{
			return m_SafeCriticalSection.Execute(() => m_RoomToRoomInfo.ContainsKey(roomId) ? (ushort)1 : (ushort)0);
		}

		#endregion

		#region Private Helper Methods

		private bool GetTargetConference(Guid conferenceId, out IConference targetConference)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				return m_Conferences.TryGetValue(conferenceId, out targetConference);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private bool GetClientIdForConference(Guid conferenceId, out uint clientId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				clientId = 0;

				IConference conference;
				if (!GetTargetConference(conferenceId, out conference))
				{
					Logger.Log(eSeverity.Error, "No Conference with the given key found.");
					return false;
				}

				ushort targetBooth = m_AdapterToBooth.Where(kvp => kvp.Key.ContainsConference(conference))
													 .Select(kvp => kvp.Value)
													 .FirstOrDefault();

				int targetRoom;
				if (!m_RoomToBooth.TryGetKey(targetBooth, out targetRoom))
				{
					Logger.Log(eSeverity.Error, "No room currently assigned to booth {0}", targetBooth);
					return false;
				}

				if (!m_ClientToRoom.TryGetKey(targetRoom, out clientId))
				{
					Logger.Log(eSeverity.Error, "No client currently registered to for room {0}", targetRoom);
					return false;
				}

				return true;
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private bool GetClientIdForAdapter(ISimplInterpretationDevice device, out uint clientId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				clientId = 0;

				ushort targetBooth;
				if (!m_AdapterToBooth.TryGetValue(device, out targetBooth))
				{
					Logger.Log(eSeverity.Error, "No booth assigned to target device {0}", device.Id);
					return false;
				}

				int targetRoom;
				if (!m_RoomToBooth.TryGetKey(targetBooth, out targetRoom))
				{
					Logger.Log(eSeverity.Error, "No room currently assigned to booth {0}", targetBooth);
					return false;
				}

				if (!m_ClientToRoom.TryGetKey(targetRoom, out clientId))
				{
					Logger.Log(eSeverity.Error, "No client currently registered to for room {0}", targetRoom);
					return false;
				}

				return true;
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private bool GetAdaptersForClientId(uint clientId, out IcdHashSet<ISimplInterpretationDevice> devices)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				devices = null;

				int room;
				if (!m_ClientToRoom.TryGetValue(clientId, out room))
				{
					Logger.Log(eSeverity.Error, "No Room assigned to Client {0}", clientId);
					return false;
				}

				ushort booth;
				if (!m_RoomToBooth.TryGetValue(room, out booth))
				{
					Logger.Log(eSeverity.Error, "No Booth assigned to Room {0}", room);
					return false;
				}

				ISimplInterpretationDevice device;
				if (!m_AdapterToBooth.TryGetKey(booth, out device))
				{
					Logger.Log(eSeverity.Error, "No Adapter is assigned to booth {0}", booth);
					return false;
				}

				devices = new IcdHashSet<ISimplInterpretationDevice>();
				foreach (var kvp in m_AdapterToBooth.Where(kvp => kvp.Value == booth))
					devices.Add(kvp.Key);

				return true;
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private bool GetAdapterForRoom(int roomId, out ISimplInterpretationDevice device)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				device = null;

				ushort targetBooth;
				if (!m_RoomToBooth.TryGetValue(roomId, out targetBooth))
				{
					Logger.Log(eSeverity.Error, "No booth assigned to room {0}", roomId);
					return false;
				}

				if (!m_AdapterToBooth.TryGetKey(targetBooth, out device))
				{
					Logger.Log(eSeverity.Error, "No booth with id {0}", targetBooth);
					return false;
				}

				return true;
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private bool GetAdaptersForRoom(int roomId, out IcdHashSet<ISimplInterpretationDevice> devices)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				devices = null;

				ushort targetBooth;
				if (!m_RoomToBooth.TryGetValue(roomId, out targetBooth))
				{
					Logger.Log(eSeverity.Error, "No booth assigned to room {0}", roomId);
					return false;
				}

				ISimplInterpretationDevice device;
				if (!m_AdapterToBooth.TryGetKey(targetBooth, out device))
				{
					Logger.Log(eSeverity.Error, "No booth with id {0}", targetBooth);
					return false;
				}

				devices = new IcdHashSet<ISimplInterpretationDevice>();
				foreach (var kvp in m_AdapterToBooth.Where(kvp => kvp.Value == targetBooth))
					devices.Add(kvp.Key);

				return true;
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		} 

		private void TransmitInterpretationState(ushort boothId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				uint clientId;
				if (!GetClientIdForAdapter(m_AdapterToBooth.GetKey(boothId), out clientId))
					return;

				m_RpcController.CallMethod(clientId, InterpretationClientDevice.SET_INTERPRETATION_STATE_RPC, true);

				IEnumerable<ISimplInterpretationDevice> adapters = m_AdapterToBooth.GetKeys(boothId);

				foreach (ISimplInterpretationDevice adapter in adapters)
				{
					foreach (IConference conference in adapter.GetConferences())
					{
						ConferenceState conferenceState = ConferenceState.FromConference(conference, adapter.Language);
						Guid id = m_Conferences.GetKey(conference);
						m_RpcController.CallMethod(clientId, InterpretationClientDevice.UPDATE_CACHED_CONFERENCE_STATE, id, conferenceState);
					}
				}
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		#endregion

		#region RPCs

		[Rpc(REGISTER_ROOM_RPC)]
		public void RegisterRoom(uint clientId, int roomId, string roomName)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				ushort boothId = GetBoothId(roomId);
				// this room was previously connected, retransmit the requisite info instead of adding a new room.
				if (m_ClientToRoom.ContainsKey(clientId) && boothId != 0)
				{
					TransmitInterpretationState(boothId);
					return;
				}

				m_ClientToRoom[clientId] = roomId;
				m_RoomToRoomInfo[roomId] = roomName;

				OnRoomAdded.Raise(this, new InterpretationRoomInfoArgs(roomId, roomName));
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		[Rpc(UNREGISTER_ROOM_RPC)]
		public void UnregisterRoom(uint clientId, int roomId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				if (!m_ClientToRoom.ContainsKey(clientId))
				{
					Logger.Log(eSeverity.Error, "Failed to unregister room - not registered");
					return;
				}

				m_ClientToRoom.Remove(clientId);
				m_RoomToBooth.Remove(roomId);
				m_RoomToRoomInfo.Remove(roomId);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		[Rpc(DIAL_RPC)]
		public void Dial(uint clientId, int roomId, string number)
		{
			ISimplInterpretationDevice device;
			if (GetAdapterForRoom(roomId, out device))
				device.Dial(number);
		}

		[Rpc(DIAL_TYPE_RPC)]
		public void Dial(uint clientId, int roomId, string number, eCallType type)
		{
			ISimplInterpretationDevice device;
			if (GetAdapterForRoom(roomId, out device))
				device.Dial(number, type);
		}

		[Rpc(AUTO_ANSWER_RPC)]
		public void SetAutoAnswer(uint clientId, int roomId, bool enabled)
		{
			IcdHashSet<ISimplInterpretationDevice> devices;
			if (!GetAdaptersForRoom(roomId, out devices))
				return;

			foreach (var device in devices)
				device.SetAutoAnswer(enabled);
		}

		[Rpc(DO_NOT_DISTURB_RPC)]
		public void SetDoNotDisturb(uint clientId, int roomId, bool enabled)
		{
			IcdHashSet<ISimplInterpretationDevice> devices;
			if (!GetAdaptersForRoom(roomId, out devices))
				return;

			foreach (var device in devices)
				device.SetDoNotDisturb(enabled);
		}

		[Rpc(PRIVACY_MUTE_RPC)]
		public void SetPrivacyMute(uint clientId, int roomId, bool enabled)
		{
			IcdHashSet<ISimplInterpretationDevice> devices;
			if (!GetAdaptersForRoom(roomId, out devices))
				return;

			foreach(var device in devices)
				device.SetPrivacyMute(enabled);
		}

		[Rpc(HOLD_ENABLE_RPC)]
		public void HoldEnable(uint clientId, int roomId, Guid id)
		{
			IConference conference;
			if (!GetTargetConference(id, out conference))
				return;

			conference.Hold();
		}

		[Rpc(HOLD_RESUME_RPC)]
		public void HoldResume(uint clientId, int roomId, Guid id)
		{
			IConference conference;
			if (!GetTargetConference(id, out conference))
				return;

			conference.Resume();
		}

		[Rpc(SEND_DTMF_RPC)]
		public void SendDtmf(uint clientId, int roomId, Guid id, string data)
		{
			IConference conference;
			if (!GetTargetConference(id, out conference))
				return;

			conference.SendDtmf(data);
		}

		[Rpc(LEAVE_CONFERENCE_RPC)]
		public void LeaveConference(uint clientId, int roomId, Guid id)
		{
			IConference conference;
			if (!GetTargetConference(id, out conference))
				return;

			conference.LeaveConference();
		}

		[Rpc(END_CONFERENCE_RPC)]
		public void EndConference(uint clientId, int roomId, Guid id)
		{
			IConference conference;
			if (!GetTargetConference(id, out conference))
				return;

			conference.EndConference();
		}

		[Rpc(START_RECORDING_RPC)]
		public void StartRecording(uint clientId, int roomId, Guid id)
		{
			IConference conference;
			if (!GetTargetConference(id, out conference))
				return;

			conference.StartRecordingConference();
		}

		[Rpc(STOP_RECORDING_RPC)]
		public void StopRecording(uint clientId, int roomId, Guid id)
		{
			IConference conference;
			if (!GetTargetConference(id, out conference))
				return;

			conference.StopRecordingConference();
		}

		[Rpc(PAUSE_RECORDING_RPC)]
		public void PauseRecording(uint clientId, int roomId, Guid id)
		{
			IConference conference;
			if (!GetTargetConference(id, out conference))
				return;

			conference.PauseRecordingConference();
		}

		#endregion

		#region Adapters

		private void AddAdapter(ushort boothId, ISimplInterpretationDevice device)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				if (device == null)
					throw new ArgumentNullException("device");

				if (m_AdapterToBooth.ContainsKey(device))
					return;

				m_AdapterToBooth.Add(device, boothId);

				Subscribe(device);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private void RemoveAdapter(ISimplInterpretationDevice device)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				if (device == null)
					throw new ArgumentNullException("device");

				if (!m_AdapterToBooth.ContainsKey(device))
					return;

				Unsubscribe(device);

				m_AdapterToBooth.Remove(device);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private void ClearAdapters()
		{
			m_SafeCriticalSection.Enter();
			try
			{
				foreach (ISimplInterpretationDevice adapter in m_AdapterToBooth.Keys.ToArray())
					Unsubscribe(adapter);

				m_AdapterToBooth.Clear();
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private void Subscribe(ISimplInterpretationDevice device)
		{
			if (device == null)
				return;

			device.OnConferenceAdded += AdapterOnConferenceAdded;
			device.OnConferenceRemoved += AdapterOnConferenceRemoved;
			device.OnAutoAnswerChanged += AdapterOnAutoAnswerChanged;
			device.OnDoNotDisturbChanged += AdapterOnDoNotDisturbChanged;
			device.OnPrivacyMuteChanged += AdapterOnPrivacyMuteChanged;

			device.OnBoothIdChanged += AdapterOnBoothIdChanged;
		}

		private void Unsubscribe(ISimplInterpretationDevice device)
		{
			if (device == null)
				return;

			device.OnConferenceAdded -= AdapterOnConferenceAdded;
			device.OnConferenceRemoved -= AdapterOnConferenceRemoved;
			device.OnAutoAnswerChanged -= AdapterOnAutoAnswerChanged;
			device.OnDoNotDisturbChanged -= AdapterOnDoNotDisturbChanged;
			device.OnPrivacyMuteChanged -= AdapterOnPrivacyMuteChanged;

			device.OnBoothIdChanged -= AdapterOnBoothIdChanged;
		}

		private void AdapterOnConferenceAdded(object sender, ConferenceEventArgs args)
		{
			AddConference(args.Data);
		}

		private void AdapterOnConferenceRemoved(object sender, ConferenceEventArgs args)
		{
			var adapter = sender as SimplInterpretationDevice;
			RemoveConference(args.Data, adapter);
		}

		private void AdapterOnAutoAnswerChanged(object sender, SPlusBoolEventArgs args)
		{
			const string key = InterpretationClientDevice.SET_CACHED_AUTO_ANSWER_STATE;

			uint clientId;
			if (!GetClientIdForAdapter(sender as ISimplInterpretationDevice, out clientId))
				return;

			m_RpcController.CallMethod(clientId, key, args.Data);
		}

		private void AdapterOnDoNotDisturbChanged(object sender, SPlusBoolEventArgs args)
		{
			const string key = InterpretationClientDevice.SET_CACHED_DO_NOT_DISTURB_STATE;

			uint clientId;
			if (!GetClientIdForAdapter(sender as ISimplInterpretationDevice, out clientId))
				return;

			m_RpcController.CallMethod(clientId, key, args.Data);
		}

		private void AdapterOnPrivacyMuteChanged(object sender, SPlusBoolEventArgs args)
		{
			const string key = InterpretationClientDevice.SET_CACHED_PRIVACY_MUTE_STATE;

			uint clientId;
			if (!GetClientIdForAdapter(sender as ISimplInterpretationDevice, out clientId))
				return;

			m_RpcController.CallMethod(clientId, key, args.Data);
		}

		private void AdapterOnBoothIdChanged(object sender, SPlusUShortEventArgs args)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				RemoveAdapter(sender as ISimplInterpretationDevice);
				AddAdapter(args.Data, sender as ISimplInterpretationDevice);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		#endregion

		#region Conferences

		private void AddConference(IConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			if (m_Conferences.ContainsValue(conference))
				return;

			Guid newId = Guid.NewGuid();
			m_SafeCriticalSection.Execute(() => m_Conferences.Add(newId, conference));
			m_SafeCriticalSection.Execute(() => m_ConferencesToParticipants.Add(conference, new IcdHashSet<IParticipant>()));

			Subscribe(conference);

			ConferenceOnPropertyChanged(conference, EventArgs.Empty);
		}

		private void RemoveConference(IConference conference)
		{
			m_SafeCriticalSection.Enter();

			try
			{
				if (!m_Conferences.ContainsValue(conference))
					return;

				Guid id = m_Conferences.GetKey(conference);

				uint clientId;
				if (!GetClientIdForConference(id, out clientId))
					return;

				const string key = InterpretationClientDevice.REMOVE_CACHED_CONFERENCE;
				m_RpcController.CallMethod(clientId, key, id);

				m_Conferences.RemoveAllValues(conference);
				m_ConferencesToParticipants.Remove(conference);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}

			Unsubscribe(conference);
		}

		/// <summary>
		/// Removes the conference, speficying the adapter (used to look up a conference the adapter has already removed)
		/// todo: un-jank this
		/// </summary>
		/// <param name="conference"></param>
		/// <param name="adapter"></param>
		private void RemoveConference(IConference conference, SimplInterpretationDevice adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			m_SafeCriticalSection.Enter();

			try
			{
				if (!m_Conferences.ContainsValue(conference))
					return;

				Guid id = m_Conferences.GetKey(conference);

				uint clientId;
				if (!GetClientIdForAdapter(adapter, out clientId))
					return;

				const string key = InterpretationClientDevice.REMOVE_CACHED_CONFERENCE;
				m_RpcController.CallMethod(clientId, key, id);

				m_Conferences.RemoveAllValues(conference);

			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}

			Unsubscribe(conference);
		}

		private void ClearSources()
		{
			m_SafeCriticalSection.Enter();
			try
			{
				foreach (IConference conference in m_Conferences.Values.ToArray(m_Conferences.Count))
					RemoveConference(conference);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}


		private void Subscribe(IConference conference)
		{
			//todo: break out participants vs properties to make sync data smaller
			conference.OnSupportedConferenceFeaturesChanged += ConferenceOnPropertyChanged;
			conference.OnStatusChanged += ConferenceOnPropertyChanged;
			conference.OnNameChanged += ConferenceOnPropertyChanged;
			conference.OnCallTypeChanged += ConferenceOnPropertyChanged;
			conference.OnConferenceRecordingStatusChanged += ConferenceOnPropertyChanged;
			conference.OnStartTimeChanged += ConferenceOnPropertyChanged;
			conference.OnEndTimeChanged += ConferenceOnPropertyChanged;
			conference.OnParticipantAdded += ConferenceOnParticipantAdded;
			conference.OnParticipantRemoved += ConferenceOnParticipantRemoved;

			m_SafeCriticalSection.Enter();
			try
			{
				foreach (IParticipant participant in conference.GetParticipants())
				{
					if (m_ConferencesToParticipants[conference].Contains(participant))
						continue;

					m_ConferencesToParticipants[conference].Add(participant);
					Subscribe(participant);
				}
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private void Unsubscribe(IConference conference)
		{
			conference.OnSupportedConferenceFeaturesChanged -= ConferenceOnPropertyChanged;
			conference.OnStatusChanged -= ConferenceOnPropertyChanged;
			conference.OnNameChanged -= ConferenceOnPropertyChanged;
			conference.OnCallTypeChanged -= ConferenceOnPropertyChanged;
			conference.OnConferenceRecordingStatusChanged -= ConferenceOnPropertyChanged;
			conference.OnStartTimeChanged -= ConferenceOnPropertyChanged;
			conference.OnEndTimeChanged -= ConferenceOnPropertyChanged;
			conference.OnParticipantAdded -= ConferenceOnParticipantAdded;
			conference.OnParticipantRemoved -= ConferenceOnParticipantRemoved;

			m_SafeCriticalSection.Enter();
			try
			{
				foreach (IParticipant participant in conference.GetParticipants())
				{
					if (!m_ConferencesToParticipants[conference].Contains(participant))
						continue;

					m_ConferencesToParticipants[conference].Remove(participant);
					Unsubscribe(participant);
				}
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private void ConferenceOnPropertyChanged(object sender, EventArgs args)
		{
			m_SafeCriticalSection.Enter();

			try
			{
				IConference conference = sender as IConference;
				if (conference == null)
					return;

				if (!m_Conferences.ContainsValue(conference))
				{
					Logger.Log(eSeverity.Error, "Unknown sourceState {0}", conference.Name);
					return;
				}

				Guid id = m_Conferences.GetKey(conference);

				uint clientId;
				if (!GetClientIdForConference(id, out clientId))
					return;

				IcdHashSet<ISimplInterpretationDevice> adapters;
				GetAdaptersForClientId(clientId, out adapters);

				ISimplInterpretationDevice targetAdapter = adapters.FirstOrDefault(a => a.ContainsConference(conference));

				ConferenceState sourceState = ConferenceState.FromConference(conference, targetAdapter != null
					                                                                             ? targetAdapter.Language
					                                                                             : null);

				m_RpcController.CallMethod(clientId, InterpretationClientDevice.UPDATE_CACHED_CONFERENCE_STATE, id, sourceState);

				if (conference.Status == eConferenceStatus.Disconnected)
					RemoveConference(conference);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private void ConferenceOnParticipantAdded(object sender, ParticipantEventArgs args)
        {
			IConference conf = sender as IConference;
			IParticipant part = args.Data;
			if (conf == null)
				return;

			m_SafeCriticalSection.Enter();
			try
			{
				if (m_ConferencesToParticipants[conf].Contains(part))
					return;

				m_ConferencesToParticipants[conf].Add(part);
				Subscribe(part);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}

			// Whenever any property changes we need to update.
			ConferenceOnPropertyChanged(sender, args);
        }

		private void ConferenceOnParticipantRemoved(object sender, ParticipantEventArgs args)
		{
			IConference conf = sender as IConference;
			IParticipant part = args.Data;
			if (conf == null)
				return;

			m_SafeCriticalSection.Enter();
			try
			{
				if (!m_ConferencesToParticipants[conf].Contains(part))
					return;

				m_ConferencesToParticipants[conf].Remove(part);
				Unsubscribe(part);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}

			// Whenever any property changes we need to update.
			ConferenceOnPropertyChanged(sender, args);
		}

		#endregion

		#region Participants

		private void Subscribe(IParticipant participant)
		{
			participant.OnStatusChanged += ParticipantOnPropertyChanged;
			participant.OnParticipantTypeChanged += ParticipantOnPropertyChanged;
			participant.OnNameChanged += ParticipantOnPropertyChanged;
			participant.OnStartTimeChanged += ParticipantOnPropertyChanged;
			participant.OnEndTimeChanged += ParticipantOnPropertyChanged;
			participant.OnNumberChanged += ParticipantOnPropertyChanged;
			participant.OnAnswerStateChanged += ParticipantOnPropertyChanged;
			participant.OnIsMutedChanged += ParticipantOnPropertyChanged;
			participant.OnIsHostChanged += ParticipantOnPropertyChanged;
			participant.OnHandRaisedChanged += ParticipantOnPropertyChanged;
			participant.OnSupportedParticipantFeaturesChanged += ParticipantOnPropertyChanged;
		}

		private void Unsubscribe(IParticipant participant)
		{
			participant.OnStatusChanged -= ParticipantOnPropertyChanged;
			participant.OnParticipantTypeChanged -= ParticipantOnPropertyChanged;
			participant.OnNameChanged -= ParticipantOnPropertyChanged;
			participant.OnStartTimeChanged -= ParticipantOnPropertyChanged;
			participant.OnEndTimeChanged -= ParticipantOnPropertyChanged;
			participant.OnNumberChanged -= ParticipantOnPropertyChanged;
			participant.OnAnswerStateChanged -= ParticipantOnPropertyChanged;
			participant.OnIsMutedChanged -= ParticipantOnPropertyChanged;
			participant.OnIsHostChanged -= ParticipantOnPropertyChanged;
			participant.OnHandRaisedChanged -= ParticipantOnPropertyChanged;
			participant.OnSupportedParticipantFeaturesChanged -= ParticipantOnPropertyChanged;
		}

		private void ParticipantOnPropertyChanged(object sender, EventArgs args)
		{
			IParticipant part = sender as IParticipant;
			if (part == null)
				return;

			IConference conf = m_ConferencesToParticipants.FirstOrDefault(kvp => kvp.Value.Contains(part)).Key;
			if (conf == null)
				return;

			ConferenceOnPropertyChanged(conf, EventArgs.Empty);
		}

		#endregion

		#region TCP Server

		private void Subscribe(IcdTcpServer server)
		{
			if (server != null)
				server.OnSocketStateChange += ServerOnSocketStateChange;
		}

		private void Unsubscribe(IcdTcpServer server)
		{
			if (server != null)
				server.OnSocketStateChange -= ServerOnSocketStateChange;
		}

		private void ServerOnSocketStateChange(object sender, SocketStateEventArgs args)
		{
			bool online = GetAreAdaptersOnlineStatus();
			SetIsOnline(online);
		}

		private bool GetAreAdaptersOnlineStatus()
		{
			return m_SafeCriticalSection.Execute(() => m_AdapterToBooth.AnyAndAll(adapter => adapter.Key.IsOnline));
		}

		#endregion

		#region Settings

		protected override void ApplySettingsFinal(InterpretationServerDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			foreach (int adapterId in settings.GetDeviceIds())
			{
				ISimplInterpretationDevice device = null;

				try
				{
					device = factory.GetOriginatorById<ISimplInterpretationDevice>(adapterId);
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No Interpretation Adapter found with Id:{0}", adapterId);
				}

				if (device == null)
					continue;

				AddAdapter(device.BoothId, device);
			}

			m_Server.Port = settings.ServerPort;
			m_Server.MaxNumberOfClients = settings.ServerMaxClients;
		}

		protected override void CopySettingsFinal(InterpretationServerDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.SetDeviceIds(m_AdapterToBooth.Keys.Select(adapter => adapter.Id));
			settings.ServerMaxClients = m_Server.MaxNumberOfClients;
			settings.ServerPort = m_Server.Port;
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_Server.Stop();

			ClearAdapters();

			ClearSources();
		}

		/// <summary>
		/// Override to add actions on StartSettings
		/// This should be used to start communications with devices and perform initial actions
		/// </summary>
		protected override void StartSettingsFinal()
		{
			base.StartSettingsFinal();

			m_Server.Start();
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (var cmd in GetBaseConsoleCommands())
				yield return cmd;

			yield return new ConsoleCommand("ListRooms", "Lists the rooms", () => ListRooms());
			yield return new ConsoleCommand("ListBooths", "Lists the booths", () => ListBooths());
			yield return new ConsoleCommand("ListInterpretations", "Lists the pairings between booths and rooms", () => ListPairs());
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private string ListRooms()
		{
			var table = new TableBuilder("Room Id", "Room Name");

			foreach (var kvp in m_RoomToRoomInfo)
				table.AddRow(kvp.Key.ToString(), kvp.Value);

			return (table.ToString());
		}

		private string ListBooths()
		{
			var table = new TableBuilder("Adapter Id", "Booth Id");

			foreach (var kvp in m_AdapterToBooth)
				table.AddRow(kvp.Key.Id, kvp.Value);

			return (table.ToString());

		}

		private string ListPairs()
		{
			var table = new TableBuilder("Room Id", "Booth Id");

			foreach (var kvp in m_RoomToBooth)
				table.AddRow(kvp.Key, kvp.Value);

			return (table.ToString());
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return m_Server;
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
