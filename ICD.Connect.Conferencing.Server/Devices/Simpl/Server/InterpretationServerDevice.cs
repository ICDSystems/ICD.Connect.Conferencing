using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Server.Devices.Client;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Server.Devices.Simpl.Server
{
	[PublicAPI]
	public sealed class InterpretationServerDevice : AbstractSimplDevice<InterpretationServerDeviceSettings>, IInterpretationServerDevice
	{
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

		// key is booth id, value is device device for that booth.
		private readonly Dictionary<int, ISimplInterpretationDevice> m_BoothToAdapter;

		// key is guid id of source, value is the source
		private readonly Dictionary<Guid, IConferenceSource> m_Sources;

		// key is room id, value is booth number
		private readonly Dictionary<int, int> m_RoomToBooth;

		// key is tcp client id, value is room id
		private readonly Dictionary<uint, int> m_ClientToRoom;

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public InterpretationServerDevice()
		{
			m_RpcController = new ServerSerialRpcController(this);
			m_BoothToAdapter = new Dictionary<int, ISimplInterpretationDevice>();
			m_Sources = new Dictionary<Guid, IConferenceSource>();
			m_RoomToBooth = new Dictionary<int, int>();
			m_ClientToRoom = new Dictionary<uint, int>();

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
			return m_ClientToRoom.Where(kvp => !m_RoomToBooth.ContainsKey(kvp.Value))
								 .Select(kvp => kvp.Value)
								 .ToArray();
		}

		/// <summary>
		/// Gets the booths that are not currently interpreting for any rooms.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<int> GetAvailableBoothIds()
		{
			IEnumerable<int> usedBooths = m_RoomToBooth.Values;
			return m_BoothToAdapter.Keys.Except(usedBooths);
		}

		/// <summary>
		/// Begins forwarding 
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="boothId"></param>
		[PublicAPI]
		public void BeginInterpretation(int roomId, int boothId)
		{
			if (!m_BoothToAdapter.ContainsKey(boothId))
				return;

			if (!m_RoomToBooth.ContainsKey(roomId))
				return;

			// No change
			if (m_RoomToBooth[roomId] == boothId)
				return;

			uint clientId;
			if (!GetClientIdForAdapter(m_BoothToAdapter[boothId], out clientId))
				return;

			m_RoomToBooth[roomId] = boothId;

			m_RpcController.CallMethod(clientId, InterpretationClientDevice.SET_INTERPRETATION_STATE_RPC, true);

			foreach (IConferenceSource source in m_BoothToAdapter[boothId].GetSources())
			{
				ConferenceSourceState sourceState = ConferenceSourceState.FromSource(source);
				Guid id = m_Sources.GetKey(source);
				m_RpcController.CallMethod(clientId, InterpretationClientDevice.UPDATE_CACHED_SOURCE_STATE, id, sourceState);
			}
		}

		[PublicAPI]
		public void EndInterpretation(int roomId, int boothId)
		{
			if (!m_BoothToAdapter.ContainsKey(boothId))
				return;

			if (!m_RoomToBooth.ContainsKey(roomId))
				return;

			// booth is not attached to the given room
			if (m_RoomToBooth[roomId] != boothId)
				return;

			uint clientId;
			if (!GetClientIdForAdapter(m_BoothToAdapter[boothId], out clientId))
				return;

			m_RoomToBooth.Remove(roomId);

			m_RpcController.CallMethod(clientId, InterpretationClientDevice.SET_INTERPRETATION_STATE_RPC, false);
		}

		#endregion

		#region Private Helper Methods

		private bool GetTargetSource(Guid sourceId, out IConferenceSource targetSource)
		{
			return m_Sources.TryGetValue(sourceId, out targetSource);
		}

		private bool GetClientIdForSource(Guid sourceId, out uint clientId)
		{
			clientId = 0;

			IConferenceSource source;
			if (!GetTargetSource(sourceId, out source))
			{
				Log(eSeverity.Error, "No Source with the given key found.");
				return false;
			}

			int targetBooth = m_BoothToAdapter.Where(kvp => kvp.Value.ContainsSource(source))
			                                  .Select(kvp => kvp.Key)
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

		private bool GetClientIdForAdapter(ISimplInterpretationDevice device, out uint clientId)
		{
			clientId = 0;

			int targetBooth;
			if (!m_BoothToAdapter.TryGetKey(device, out targetBooth))
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

		private bool GetAdapterForRoom(int roomId, out ISimplInterpretationDevice device)
		{
			device = null;

			int targetBooth;
			if (!m_RoomToBooth.TryGetValue(roomId, out targetBooth))
			{
				Log(eSeverity.Error, "No booth assigned to room {0}", roomId);
				return false;
			}

			if (!m_BoothToAdapter.TryGetValue(targetBooth, out device))
			{
				Log(eSeverity.Error, "No booth with id {0}", targetBooth);
				return false;
			}

			return true;
		}

		#endregion

		#region RPCs

		[Rpc(REGISTER_ROOM_RPC), UsedImplicitly]
		public void RegisterRoom(uint clientId, int roomId)
		{
			if (m_ClientToRoom.ContainsKey(clientId))
			{
				Log(eSeverity.Error, "Failed to register room - already registered");
				return;
			}	

			m_ClientToRoom.Add(clientId, roomId);
		}

		[Rpc(UNREGISTER_ROOM_RPC), UsedImplicitly]
		public void UnregisterRoom(uint clientId, int roomId)
		{
			if (!m_ClientToRoom.ContainsKey(clientId))
			{
				Log(eSeverity.Error, "Failed to unregister room - not registered");
				return;
			}	

			m_ClientToRoom.Remove(clientId);
			m_RoomToBooth.Remove(roomId);
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

		private void AddAdapter(int boothId, ISimplInterpretationDevice device)
		{
			if (device == null)
				throw new ArgumentNullException("device");

			if (m_BoothToAdapter.ContainsValue(device))
				return;

			m_BoothToAdapter.Add(boothId, device);

			Subscribe(device);
		}

		private void ClearAdapters()
		{
			foreach (ISimplInterpretationDevice adapter in m_BoothToAdapter.Values.ToArray())
				Unsubscribe(adapter);

			m_BoothToAdapter.Clear();
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
		}

		private void AdapterOnSourceAdded(object sender, ConferenceSourceEventArgs args)
		{
			AddSource(args.Data);
		}

		private void AdapterOnSourceRemoved(object sender, ConferenceSourceEventArgs args)
		{
			RemoveSource(args.Data);
		}

		private void AdapterOnAutoAnswerChanged(object sender, BoolEventArgs args)
		{
			const string key = InterpretationClientDevice.SET_CACHED_AUTO_ANSWER_STATE;

			uint clientId;
			if (!GetClientIdForAdapter(sender as ISimplInterpretationDevice, out clientId))
				return;

			m_RpcController.CallMethod(clientId, key, args.Data);
		}

		private void AdapterOnDoNotDisturbChanged(object sender, BoolEventArgs args)
		{
			const string key = InterpretationClientDevice.SET_CACHED_DO_NOT_DISTURB_STATE;

			uint clientId;
			if (!GetClientIdForAdapter(sender as ISimplInterpretationDevice, out clientId))
				return;

			m_RpcController.CallMethod(clientId, key, args.Data);
		}

		private void AdapterOnPrivacyMuteChanged(object sender, BoolEventArgs args)
		{
			const string key = InterpretationClientDevice.SET_CACHED_PRIVACY_MUTE_STATE;

			uint clientId;
			if (!GetClientIdForAdapter(sender as ISimplInterpretationDevice, out clientId))
				return;

			m_RpcController.CallMethod(clientId, key, args.Data);
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
			m_Sources.Add(newId, source);

			Subscribe(source);
		}

		private void RemoveSource(IConferenceSource source)
		{
			if (!m_Sources.ContainsValue(source))
				return;

			m_Sources.RemoveAllValues(source);

			Unsubscribe(source);
		}

		private void ClearSources()
		{
			foreach (IConferenceSource source in m_Sources.Values.ToArray(m_Sources.Count))
				RemoveSource(source);
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
			IsOnline = GetIsOnlineStatus();

			if (args.SocketState == SocketStateEventArgs.eSocketStatus.SocketStatusConnected)
				return;

			if (!m_ClientToRoom.ContainsKey(args.ClientId))
				return;

			int roomId = m_ClientToRoom[args.ClientId];
			UnregisterRoom(args.ClientId, roomId);
		}

		private bool GetIsOnlineStatus()
		{
			return m_BoothToAdapter.AnyAndAll(adapter => adapter.Value.IsOnline);
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

			settings.SetDeviceIds(m_BoothToAdapter.Values.Select(adapter => adapter.Id));
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
	}
}
