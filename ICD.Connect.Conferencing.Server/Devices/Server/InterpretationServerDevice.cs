﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Server.Devices.Client;
using ICD.Connect.Conferencing.Server.Devices.Simpl;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Settings.Core;
using ICD.Connect.Settings.SPlusShims.EventArguments;

namespace ICD.Connect.Conferencing.Server.Devices.Server
{
	[PublicAPI]
	public sealed class InterpretationServerDevice : AbstractSimplDevice<InterpretationServerDeviceSettings>, IInterpretationServerDevice
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

		public const string ANSWER_RPC = "Answer";
		public const string HOLD_ENABLE_RPC = "HoldEnable";
		public const string HOLD_RESUME_RPC = "HoldResume";
		public const string SEND_DTMF_RPC = "SendDTMF";
		public const string END_CALL_RPC = "EndCall";

		#endregion

		#region Private Members

		private readonly AsyncTcpServer m_Server;
		private readonly ServerSerialRpcController m_RpcController;

		private readonly SafeCriticalSection m_SafeCriticalSection;

		// key is interpretation device, value is booth id for that device.
		private readonly Dictionary<ISimplInterpretationDevice, ushort> m_AdapterToBooth;

		// key is guid id of source, value is the source
		private readonly Dictionary<Guid, IConferenceSource> m_Sources;

		// key is room id, value is booth number
		private readonly Dictionary<int, ushort> m_RoomToBooth;

		// key is tcp client id, value is room id
		private readonly Dictionary<uint, int> m_ClientToRoom;

		// key is room id, value is 2 item array with name, and prefix (in that order)
		private readonly Dictionary<int, string[]> m_RoomToRoomInfo;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public InterpretationServerDevice()
		{
			m_RpcController = new ServerSerialRpcController(this);
			m_AdapterToBooth = new Dictionary<ISimplInterpretationDevice, ushort>();
			m_Sources = new Dictionary<Guid, IConferenceSource>();
			m_RoomToBooth = new Dictionary<int, ushort>();
			m_ClientToRoom = new Dictionary<uint, int>();
			m_RoomToRoomInfo = new Dictionary<int, string[]>();

			m_SafeCriticalSection = new SafeCriticalSection();

			m_Server = new AsyncTcpServer();
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
				return m_RoomToRoomInfo.ContainsKey(roomId) ? m_RoomToRoomInfo[roomId][0] : string.Empty;
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		/// <summary>
		/// Gets the Room Prefix for a given Room Id
		/// </summary>
		/// <param name="roomId"></param>
		/// <returns></returns>
		public string GetRoomPrefix(int roomId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				return m_RoomToRoomInfo.ContainsKey(roomId) ? m_RoomToRoomInfo[roomId][1] : string.Empty;
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

		private bool GetTargetSource(Guid sourceId, out IConferenceSource targetSource)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				return m_Sources.TryGetValue(sourceId, out targetSource);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private bool GetClientIdForSource(Guid sourceId, out uint clientId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				clientId = 0;

				IConferenceSource source;
				if (!GetTargetSource(sourceId, out source))
				{
					Log(eSeverity.Error, "No Source with the given key found.");
					return false;
				}

				ushort targetBooth = m_AdapterToBooth.Where(kvp => kvp.Key.ContainsSource(source))
													 .Select(kvp => kvp.Value)
													 .FirstOrDefault();

				int targetRoom;
				if (!m_RoomToBooth.TryGetKey(targetBooth, out targetRoom))
				{
					Log(eSeverity.Error, "No room currently assigned to booth {0}", targetBooth);
					return false;
				}

				if (!m_ClientToRoom.TryGetKey(targetRoom, out clientId))
				{
					Log(eSeverity.Error, "No client currently registered to for room {0}", targetRoom);
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
					Log(eSeverity.Error, "No booth assigned to target device {0}", device.Id);
					return false;
				}

				int targetRoom;
				if (!m_RoomToBooth.TryGetKey(targetBooth, out targetRoom))
				{
					Log(eSeverity.Error, "No room currently assigned to booth {0}", targetBooth);
					return false;
				}

				if (!m_ClientToRoom.TryGetKey(targetRoom, out clientId))
				{
					Log(eSeverity.Error, "No client currently registered to for room {0}", targetRoom);
					return false;
				}

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
					Log(eSeverity.Error, "No booth assigned to room {0}", roomId);
					return false;
				}

				if (!m_AdapterToBooth.TryGetKey(targetBooth, out device))
				{
					Log(eSeverity.Error, "No booth with id {0}", targetBooth);
					return false;
				}

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
					foreach (IConferenceSource source in adapter.GetSources())
					{
						ConferenceSourceState sourceState = ConferenceSourceState.FromSource(source);
						sourceState.Language = adapter.Language;
						Guid id = m_Sources.GetKey(source);
						m_RpcController.CallMethod(clientId, InterpretationClientDevice.UPDATE_CACHED_SOURCE_STATE, id, sourceState);
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

		[Rpc(REGISTER_ROOM_RPC), UsedImplicitly]
		public void RegisterRoom(uint clientId, int roomId, string roomName, string roomPrefix)
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
				m_RoomToRoomInfo[roomId] = new[] { roomName, roomPrefix };

				OnRoomAdded.Raise(this, new InterpretationRoomInfoArgs(roomId, roomName, roomPrefix));
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		[Rpc(UNREGISTER_ROOM_RPC), UsedImplicitly]
		public void UnregisterRoom(uint clientId, int roomId)
		{
			m_SafeCriticalSection.Enter();
			try
			{
				if (!m_ClientToRoom.ContainsKey(clientId))
				{
					Log(eSeverity.Error, "Failed to unregister room - not registered");
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

		[Rpc(DIAL_RPC), UsedImplicitly]
		public void Dial(uint clientId, int roomId, string number)
		{
			ISimplInterpretationDevice device;
			if (GetAdapterForRoom(roomId, out device))
				device.Dial(number);
		}

		[Rpc(DIAL_TYPE_RPC), UsedImplicitly]
		public void Dial(uint clientId, int roomId, string number, eConferenceSourceType type)
		{
			ISimplInterpretationDevice device;
			if (GetAdapterForRoom(roomId, out device))
				device.Dial(number, type);
		}

		[Rpc(AUTO_ANSWER_RPC), UsedImplicitly]
		public void SetAutoAnswer(uint clientId, int roomId, bool enabled)
		{
			ISimplInterpretationDevice device;
			if (GetAdapterForRoom(roomId, out device))
				device.SetAutoAnswer(enabled);
		}

		[Rpc(DO_NOT_DISTURB_RPC), UsedImplicitly]
		public void SetDoNotDisturb(uint clientId, int roomId, bool enabled)
		{
			ISimplInterpretationDevice device;
			if (GetAdapterForRoom(roomId, out device))
				device.SetDoNotDisturb(enabled);
		}

		[Rpc(PRIVACY_MUTE_RPC), UsedImplicitly]
		public void SetPrivacyMute(uint clientId, int roomId, bool enabled)
		{
			ISimplInterpretationDevice device;
			if (GetAdapterForRoom(roomId, out device))
				device.SetPrivacyMute(enabled);
		}

		[Rpc(ANSWER_RPC), UsedImplicitly]
		public void Answer(uint clientId, int roomId, Guid id)
		{
			IConferenceSource source;
			if (!GetTargetSource(id, out source))
				return;

			source.Answer();
		}

		[Rpc(HOLD_ENABLE_RPC), UsedImplicitly]
		public void HoldEnable(uint clientId, int roomId, Guid id)
		{
			IConferenceSource source;
			if (!GetTargetSource(id, out source))
				return;

			source.Hold();
		}

		[Rpc(HOLD_RESUME_RPC), UsedImplicitly]
		public void HoldResume(uint clientId, int roomId, Guid id)
		{
			IConferenceSource source;
			if (!GetTargetSource(id, out source))
				return;

			source.Resume();
		}

		[Rpc(SEND_DTMF_RPC), UsedImplicitly]
		public void SendDtmf(uint clientId, int roomId, Guid id, string data)
		{
			IConferenceSource source;
			if (!GetTargetSource(id, out source))
				return;

			source.SendDtmf(data);
		}

		[Rpc(END_CALL_RPC), UsedImplicitly]
		public void EndCall(uint clientId, int roomId, Guid id)
		{
			IConferenceSource source;
			if (!GetTargetSource(id, out source))
				return;

			source.Hangup();
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

			device.OnSourceAdded += AdapterOnSourceAdded;
			device.OnSourceRemoved += AdapterOnSourceRemoved;
			device.OnAutoAnswerChanged += AdapterOnAutoAnswerChanged;
			device.OnDoNotDisturbChanged += AdapterOnDoNotDisturbChanged;
			device.OnPrivacyMuteChanged += AdapterOnPrivacyMuteChanged;

			device.OnBoothIdChanged += AdapterOnBoothIdChanged;
		}

		private void Unsubscribe(ISimplInterpretationDevice device)
		{
			if (device == null)
				return;

			device.OnSourceAdded -= AdapterOnSourceAdded;
			device.OnSourceRemoved -= AdapterOnSourceRemoved;
			device.OnAutoAnswerChanged -= AdapterOnAutoAnswerChanged;
			device.OnDoNotDisturbChanged -= AdapterOnDoNotDisturbChanged;
			device.OnPrivacyMuteChanged -= AdapterOnPrivacyMuteChanged;

			device.OnBoothIdChanged -= AdapterOnBoothIdChanged;
		}

		private void AdapterOnSourceAdded(object sender, ConferenceSourceEventArgs args)
		{
			AddSource(args.Data);
		}

		private void AdapterOnSourceRemoved(object sender, ConferenceSourceEventArgs args)
		{
			var adapter = sender as SimplInterpretationDevice;
			RemoveSource(args.Data, adapter);
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

		#region Sources

		private void AddSource(IConferenceSource source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			if (m_Sources.ContainsValue(source))
				return;

			Guid newId = Guid.NewGuid();
			m_SafeCriticalSection.Execute(() => m_Sources.Add(newId, source));

			Subscribe(source);

			SourceOnPropertyChanged(source, EventArgs.Empty);
		}

		private void RemoveSource(IConferenceSource source)
		{
			m_SafeCriticalSection.Enter();

			try
			{
				if (!m_Sources.ContainsValue(source))
					return;

				Guid id = m_Sources.GetKey(source);

				uint clientId;
				if (!GetClientIdForSource(id, out clientId))
					return;

				const string key = InterpretationClientDevice.REMOVE_CACHED_SOURCE;
				m_RpcController.CallMethod(clientId, key, id);

				m_Sources.RemoveAllValues(source);

			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}

			Unsubscribe(source);
		}

		/// <summary>
		/// Removes the source, speficying the adapter (used to look up a source the adapter has already removed)
		/// todo: un-jank this
		/// </summary>
		/// <param name="source"></param>
		/// <param name="adapter"></param>
		private void RemoveSource(IConferenceSource source, SimplInterpretationDevice adapter)
		{
			if (adapter == null)
				throw new ArgumentNullException("adapter");

			m_SafeCriticalSection.Enter();

			try
			{
				if (!m_Sources.ContainsValue(source))
					return;

				Guid id = m_Sources.GetKey(source);

				uint clientId;
				if (!GetClientIdForAdapter(adapter, out clientId))
					return;

				const string key = InterpretationClientDevice.REMOVE_CACHED_SOURCE;
				m_RpcController.CallMethod(clientId, key, id);

				m_Sources.RemoveAllValues(source);

			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}

			Unsubscribe(source);
		}

		private void ClearSources()
		{
			m_SafeCriticalSection.Enter();
			try
			{
				foreach (IConferenceSource source in m_Sources.Values.ToArray(m_Sources.Count))
					RemoveSource(source);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		private void Subscribe(IConferenceSource source)
		{
			source.OnStatusChanged += SourceOnPropertyChanged;
			source.OnAnswerStateChanged += SourceOnPropertyChanged;
			source.OnNameChanged += SourceOnPropertyChanged;
			source.OnNumberChanged += SourceOnPropertyChanged;
			source.OnSourceTypeChanged += SourceOnPropertyChanged;
		}

		private void Unsubscribe(IConferenceSource source)
		{
			source.OnStatusChanged -= SourceOnPropertyChanged;
			source.OnAnswerStateChanged -= SourceOnPropertyChanged;
			source.OnNameChanged -= SourceOnPropertyChanged;
			source.OnNumberChanged -= SourceOnPropertyChanged;
			source.OnSourceTypeChanged -= SourceOnPropertyChanged;
		}

		private void SourceOnPropertyChanged(object sender, EventArgs args)
		{
			m_SafeCriticalSection.Enter();

			try
			{
				IConferenceSource source = sender as IConferenceSource;
				if (source == null)
					return;

				if (!m_Sources.ContainsValue(source))
				{
					Log(eSeverity.Error, "Unknown sourceState {0}", source.Name);
					return;
				}

				Guid id = m_Sources.GetKey(source);

				uint clientId;
				if (!GetClientIdForSource(id, out clientId))
					return;

				ConferenceSourceState sourceState = ConferenceSourceState.FromSource(source);

				const string key = InterpretationClientDevice.UPDATE_CACHED_SOURCE_STATE;
				m_RpcController.CallMethod(clientId, key, id, sourceState);

				if (source.Status == eConferenceSourceStatus.Disconnected)
					RemoveSource(source);
			}
			finally
			{
				m_SafeCriticalSection.Leave();
			}
		}

		#endregion

		#region TCP Server

		private void Subscribe(AsyncTcpServer server)
		{
			if (server != null)
				server.OnSocketStateChange += ServerOnSocketStateChange;
		}

		private void Unsubscribe(AsyncTcpServer server)
		{
			if (server != null)
				server.OnSocketStateChange -= ServerOnSocketStateChange;
		}

		private void ServerOnSocketStateChange(object sender, SocketStateEventArgs args)
		{
			UpdateCachedIsOnlineStatus();
		}

		private void UpdateCachedIsOnlineStatus()
		{
			IsOnline = GetAreAdaptersOnlineStatus();
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
					Log(eSeverity.Error, "No Interpretation Adapter found with Id:{0}", adapterId);
				}

				if (device == null)
					continue;

				AddAdapter(device.BoothId, device);
			}

			m_Server.Port = settings.ServerPort;
			m_Server.MaxNumberOfClients = settings.ServerMaxClients;
			m_Server.Start();
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
			var table = new TableBuilder("Room Id", "Room Name", "Prefix");

			foreach (var kvp in m_RoomToRoomInfo)
				table.AddRow(kvp.Key.ToString(), kvp.Value[0], kvp.Value[1]);

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
