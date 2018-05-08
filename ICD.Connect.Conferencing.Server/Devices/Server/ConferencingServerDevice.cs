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
using ICD.Connect.Conferencing.Server.Devices.Simpl;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Simpl;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Server.Devices.Server
{
	[PublicAPI]
	public sealed class ConferencingServerDevice : AbstractDevice<ConferencingServerDeviceSettings>, IConferencingServerDevice
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

		private AsyncTcpServer m_Server;
		private readonly ServerSerialRpcController m_RpcController;

		// key is booth id, value is adapter device for that booth.
		private readonly Dictionary<int, IInterpretationAdapter> m_Adapters;

		// key is guid id of source, value is the source
		private readonly Dictionary<Guid, IConferenceSource> m_Sources;

		// key is room id, value is booth number
		private readonly Dictionary<int, int> m_RoomToBooth;

		// key is tcp client id, value is room id
		private readonly Dictionary<uint, int> m_ClientToRoom;

		#endregion

		public ConferencingServerDevice()
		{
			m_RpcController = new ServerSerialRpcController(this);
			m_Adapters = new Dictionary<int, IInterpretationAdapter>();
			m_Sources = new Dictionary<Guid, IConferenceSource>();
			m_RoomToBooth = new Dictionary<int, int>();
			m_ClientToRoom = new Dictionary<uint, int>();

			SetServer(new AsyncTcpServer());
		}

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			SetServer(null);

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
			return m_Adapters.Keys.Except(usedBooths);
		}

		/// <summary>
		/// Begins forwarding 
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="boothId"></param>
		[PublicAPI]
		public void BeginInterpretation(int roomId, int boothId)
		{
			if (!m_Adapters.ContainsKey(boothId))
				return;

			if (!m_RoomToBooth.ContainsKey(roomId))
				return;

			// No change
			if (m_RoomToBooth[roomId] == boothId)
				return;

			uint clientId;
			if (!TrygetClientIdForAdapter(m_Adapters[boothId], out clientId))
				return;

			m_RoomToBooth[roomId] = boothId;

			m_RpcController.CallMethod(clientId, ConferencingClientDevice.SET_INTERPRETATION_STATE_RPC, true);

			foreach (var source in m_Adapters[boothId].GetSources())
			{
				ConferenceSourceState sourceState = ConferenceSourceState.FromSource(source);
				Guid id = m_Sources.GetKey(source);
				m_RpcController.CallMethod(clientId, ConferencingClientDevice.UPDATE_CACHED_SOURCE_STATE, id, sourceState);
			}
		}

		[PublicAPI]
		public void EndInterpretation(int roomId, int boothId)
		{
			if (!m_Adapters.ContainsKey(boothId))
				return;

			if (!m_RoomToBooth.ContainsKey(roomId))
				return;

			// booth is not attached to the given room
			if (m_RoomToBooth[roomId] != boothId)
				return;

			uint clientId;
			if (!TrygetClientIdForAdapter(m_Adapters[boothId], out clientId))
				return;

			m_RoomToBooth.Remove(roomId);

			m_RpcController.CallMethod(clientId, ConferencingClientDevice.SET_INTERPRETATION_STATE_RPC, false);
		}

		#endregion

		#region Private Helper Methods

		/// <summary>
		/// Logs to logging core.
		/// </summary>
		/// <param name="severity"></param>
		/// <param name="message"></param>
		/// <param name="args"></param>
		private void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format(message, args);
			message = string.Format("{0} - {1}", GetType().Name, message);

			Logger.AddEntry(severity, message);
		}

		private IEnumerable<IConferenceSource> GetConferenceSourcesForBooth(int boothId)
		{
			if (!m_Adapters.ContainsKey(boothId))
				return Enumerable.Empty<IConferenceSource>();

			return m_Adapters[boothId].GetSources();
		}

		private bool TryGetTargetSource(int roomId, Guid sourceId, out IConferenceSource targetSource)
		{
			targetSource = null;

			int boothId;
			if (!m_RoomToBooth.TryGetValue(roomId, out boothId))
			{
				Log(eSeverity.Error, "No booth is assigned to room {0}", roomId);
				return false;
			}

			if (!m_Sources.TryGetValue(sourceId, out targetSource))
			{
				Log(eSeverity.Error, "No Source with the given key found.");
				return false;
			}

			return targetSource != null;
		}

		private bool TryGetClientIdForSource(Guid sourceId, out uint clientId)
		{
			clientId = 0;

			IConferenceSource source;
			if (!m_Sources.TryGetValue(sourceId, out source))
			{
				Log(eSeverity.Error, "No Source with the given key found.");
				return false;
			}

			int targetBooth = m_Adapters.Where(booth => booth.Value.GetSources().Any(src => source == src))
										.Select(booth => booth.Key)
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

			return clientId != 0;
		}

		private bool TrygetClientIdForAdapter(IInterpretationAdapter adapter, out uint clientId)
		{
			clientId = 0;

			int targetBooth;
			if (!m_Adapters.TryGetKey(adapter, out targetBooth))
			{
				Log(eSeverity.Error, "No booth assigned to target adapter {0}", adapter.Id);
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

			return clientId != 0;
		}

		private bool TryGetAdapterForRoom(int roomId, out IInterpretationAdapter adapter)
		{
			adapter = null;

			int targetBooth;
			if (!m_RoomToBooth.TryGetValue(roomId, out targetBooth))
			{
				Log(eSeverity.Error, "No booth assigned to room {0}", roomId);
				return false;
			}

			if (!m_Adapters.TryGetValue(targetBooth, out adapter))
			{
				Log(eSeverity.Error, "No booth with id {0}", targetBooth);
				return false;
			}

			return adapter != null;

		}

		#endregion

		#region RPCs

		[Rpc(REGISTER_ROOM_RPC), UsedImplicitly]
		public void RegisterRoom(uint clientId, int roomId)
		{
			if (m_ClientToRoom.ContainsKey(clientId))
				return;

			if (m_RoomToBooth.ContainsKey(roomId))
				return;

			m_ClientToRoom.Add(clientId, roomId);
		}

		[Rpc(UNREGISTER_ROOM_RPC), UsedImplicitly]
		public void UnregisterRoom(uint clientId, int roomId)
		{
			if (!m_ClientToRoom.ContainsKey(clientId))
				return;

			if (!m_RoomToBooth.ContainsKey(roomId))
				return;

			m_ClientToRoom.Remove(clientId);

			m_RoomToBooth.Remove(roomId);
		}

		[Rpc(DIAL_RPC), UsedImplicitly]
		public void Dial(uint clientId, int roomId, string number)
		{
			IInterpretationAdapter adapter;
			if (TryGetAdapterForRoom(roomId, out adapter))
				adapter.Dial(number);
		}

		[Rpc(DIAL_TYPE_RPC), UsedImplicitly]
		public void Dial(uint clientId, int roomId, string number, eConferenceSourceType type)
		{
			IInterpretationAdapter adapter;
			if (TryGetAdapterForRoom(roomId, out adapter))
				adapter.Dial(number, type);
		}

		[Rpc(AUTO_ANSWER_RPC), UsedImplicitly]
		public void SetAutoAnswer(uint clientId, int roomId, bool enabled)
		{
			IInterpretationAdapter adapter;
			if (TryGetAdapterForRoom(roomId, out adapter))
				adapter.SetAutoAnswer(enabled);
		}

		[Rpc(DO_NOT_DISTURB_RPC), UsedImplicitly]
		public void SetDoNotDisturb(uint clientId, int roomId, bool enabled)
		{
			IInterpretationAdapter adapter;
			if (TryGetAdapterForRoom(roomId, out adapter))
				adapter.SetDoNotDisturb(enabled);
		}

		[Rpc(PRIVACY_MUTE_RPC), UsedImplicitly]
		public void SetPrivacyMute(uint clientId, int roomId, bool enabled)
		{
			IInterpretationAdapter adapter;
			if (TryGetAdapterForRoom(roomId, out adapter))
				adapter.SetPrivacyMute(enabled);
		}

		[Rpc(ANSWER_RPC), UsedImplicitly]
		public void Answer(uint clientId, int roomId, Guid id)
		{
			IConferenceSource source;
			if (!TryGetTargetSource(roomId, id, out source))
				return;

			IInterpretationAdapter adapter;
			if (TryGetAdapterForRoom(roomId, out adapter))
				adapter.Answer(source);
		}

		[Rpc(HOLD_ENABLE_RPC), UsedImplicitly]
		public void HoldEnable(uint clientId, int roomId, Guid id)
		{
			IConferenceSource source;
			if (!TryGetTargetSource(roomId, id, out source))
				return;

			IInterpretationAdapter adapter;
			if (TryGetAdapterForRoom(roomId, out adapter))
				adapter.SetHold(source, true);
		}

		[Rpc(HOLD_RESUME_RPC), UsedImplicitly]
		public void HoldResume(uint clientId, int roomId, Guid id)
		{
			IConferenceSource source;
			if (!TryGetTargetSource(roomId, id, out source))
				return;

			IInterpretationAdapter adapter;
			if (TryGetAdapterForRoom(roomId, out adapter))
				adapter.SetHold(source, false);
		}

		[Rpc(SEND_DTMF_RPC), UsedImplicitly]
		public void SendDtmf(uint clientId, int roomId, Guid id, string data)
		{
			IConferenceSource source;
			if (!TryGetTargetSource(roomId, id, out source))
				return;

			IInterpretationAdapter adapter;
			if (TryGetAdapterForRoom(roomId, out adapter))
				adapter.SendDtmf(source, data);
		}

		[Rpc(END_CALL_RPC), UsedImplicitly]
		public void EndCall(uint clientId, int roomId, Guid id)
		{
			IConferenceSource source;
			if (!TryGetTargetSource(roomId, id, out source))
				return;

			IInterpretationAdapter adapter;
			if (TryGetAdapterForRoom(roomId, out adapter))
				adapter.EndCall(source);
		}

		#endregion

		#region Adapters

		private void AddAdapter(int boothId, IInterpretationAdapter adapter)
		{
			if (m_Adapters.ContainsValue(adapter))
				return;

			m_Adapters.Add(boothId, adapter);

			Subscribe(adapter);
		}

		private void ClearAdapters()
		{
			foreach (IInterpretationAdapter adapter in m_Adapters.Values.ToArray())
				Unsubscribe(adapter);

			m_Adapters.Clear();
		}

		private void Subscribe(IInterpretationAdapter adapter)
		{
			if (adapter == null)
				return;

			adapter.OnSourceAdded += AdapterOnSourceAdded;
			adapter.OnSourceRemoved += AdapterOnSourceRemoved;

			adapter.OnAutoAnswerChanged += AdapterOnAutoAnswerChanged;
			adapter.OnDoNotDisturbChanged += AdapterOnDoNotDisturbChanged;
			adapter.OnPrivacyMuteChanged += AdapterOnPrivacyMuteChanged;
		}

		private void Unsubscribe(IInterpretationAdapter adapter)
		{
			if (adapter == null)
				return;

			adapter.OnSourceAdded -= AdapterOnSourceAdded;
			adapter.OnSourceRemoved -= AdapterOnSourceRemoved;

			adapter.OnAutoAnswerChanged -= AdapterOnAutoAnswerChanged;
			adapter.OnDoNotDisturbChanged -= AdapterOnDoNotDisturbChanged;
			adapter.OnPrivacyMuteChanged -= AdapterOnPrivacyMuteChanged;
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
			const string key = ConferencingClientDevice.SET_CACHED_AUTO_ANSWER_STATE;

			uint clientId;
			if (!TrygetClientIdForAdapter(sender as IInterpretationAdapter, out clientId))
				return;

			m_RpcController.CallMethod(clientId, key, args.Data);
		}

		private void AdapterOnDoNotDisturbChanged(object sender, BoolEventArgs args)
		{
			const string key = ConferencingClientDevice.SET_CACHED_DO_NOT_DISTURB_STATE;

			uint clientId;
			if (!TrygetClientIdForAdapter(sender as IInterpretationAdapter, out clientId))
				return;

			m_RpcController.CallMethod(clientId, key, args.Data);
		}

		private void AdapterOnPrivacyMuteChanged(object sender, BoolEventArgs args)
		{
			const string key = ConferencingClientDevice.SET_CACHED_PRIVACY_MUTE_STATE;

			uint clientId;
			if (!TrygetClientIdForAdapter(sender as IInterpretationAdapter, out clientId))
				return;

			m_RpcController.CallMethod(clientId, key, args.Data);
		}

		#endregion

		#region Sources

		private void AddSource(IConferenceSource source)
		{
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
			if (!TryGetClientIdForSource(id, out clientId))
				return;

			ConferenceSourceState sourceState = ConferenceSourceState.FromSource(source);

			const string key = ConferencingClientDevice.UPDATE_CACHED_SOURCE_STATE;
			m_RpcController.CallMethod(clientId, key, id, sourceState);

			if (source.Status == eConferenceSourceStatus.Disconnected)
				RemoveSource(source);
		}

		#endregion

		#region TCP Server

		/// <summary>
		/// Sets the server for communication with clients.
		/// </summary>
		/// <param name="server"></param>
		[PublicAPI]
		public void SetServer(AsyncTcpServer server)
		{
			if (server == m_Server)
				return;

			Unsubscribe(m_Server);
			m_Server = server;
			m_RpcController.SetServer(m_Server);
			Subscribe(m_Server);
		}

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
			UpdateCachedOnlineStatus();

			if (args.SocketState == SocketStateEventArgs.eSocketStatus.SocketStatusConnected)
				return;

			if (!m_ClientToRoom.ContainsKey(args.ClientId))
				return;

			int roomId = m_ClientToRoom[args.ClientId];
			UnregisterRoom(args.ClientId, roomId);
		}

		#endregion

		#region Settings

		protected override void ApplySettingsFinal(ConferencingServerDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			foreach (int adapterId in settings.WrappedDeviceIds)
			{
				IInterpretationAdapter adapter = null;

				try
				{
					adapter = factory.GetOriginatorById<IInterpretationAdapter>(adapterId);
				}
				catch (KeyNotFoundException)
				{
					Logger.AddEntry(eSeverity.Error, "No Interpretation Adapter found with Id:{0}", adapterId);
				}

				if (adapter == null)
					continue;

				AddAdapter(adapter.BoothId, adapter);
			}

			m_Server.Port = settings.ServerPort;
			m_Server.MaxNumberOfClients = settings.ServerMaxClients;
			m_Server.Start();
		}

		protected override void CopySettingsFinal(ConferencingServerDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.WrappedDeviceIds = m_Adapters.Values.Select(adapter => adapter.Id).ToList();
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_Server.Stop();

			ClearAdapters();

			ClearSources();
		}

		#endregion

		#region IDevice

		protected override bool GetIsOnlineStatus()
		{
			return m_Adapters.AnyAndAll(adapter => adapter.Value.IsOnline);
		}

		#endregion

		#region ISimplDevice

		/// <summary>
		/// Gets/sets the online status callback.
		/// </summary>
		public SimplDeviceOnlineCallback OnlineStatusCallback { get; set; }

		#endregion
	}
}