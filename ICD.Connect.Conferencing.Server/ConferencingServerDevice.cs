using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Server
{
	[PublicAPI]
	public sealed class ConferencingServerDevice : AbstractDevice<ConferencingServerDeviceSettings>
	{
		#region RPC Constants

		public const string REGISTER_ROOM_RPC = "RegisterRoom";
		public const string UNREGISTER_ROOM_RPC = "UnregisterRoom";

		public const string PRIVACY_MUTE_RPC = "PrivacyMute";
		public const string AUTO_ANSWER_RPC = "AutoAnswer";
		public const string DO_NOT_DISTURB_RPC = "DoNotDisturb";

		public const string ANSWER_RPC = "Answer";

		public const string HOLD_ENABLE_RPC = "HoldEnable";
		public const string HOLD_RESUME_RPC = "HoldResume";

		public const string END_CALL_RPC = "EndCall";

		public const string SEND_DTMF_RPC = "SendDTMF";

		#endregion

		#region Private Members

		private AsyncTcpServer m_Server;
		private readonly ServerSerialRpcController m_RpcController;

		// key is booth id, value is booth object
		private readonly Dictionary<int, IInterpreterBooth> m_Booths;

		// key is booth id, value is adapter device for that booth.
		private readonly Dictionary<int, IInterpretationAdapter> m_Adapters;

		// key is guid id of source, value is the source
		private readonly Dictionary<Guid, IConferenceSource> m_Sources;

		// key is room id, value is booth number or null if no booth
		private readonly Dictionary<int, int?> m_Rooms;

		// key is tcp client id, value is room id
		private readonly Dictionary<uint, int> m_Clients; 

		#endregion

		public ConferencingServerDevice()
		{
			m_RpcController = new ServerSerialRpcController(this);
			m_Booths = new Dictionary<int, IInterpreterBooth>();
			m_Adapters = new Dictionary<int, IInterpretationAdapter>();
			m_Sources = new Dictionary<Guid, IConferenceSource>();
			m_Rooms = new Dictionary<int, int?>();
			m_Clients = new Dictionary<uint, int>();

			SetServer(new AsyncTcpServer());
		}

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			SetServer(null);

			ClearAdapters();
			ClearBooths();
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
			return m_Clients.Where(kvp => !m_Rooms.ContainsKey(kvp.Value))
			                .Select(kvp => kvp.Value)
			                .ToArray();

			//return m_Rooms.Where(kvp => kvp.Value == null).Select(kvp => kvp.Key).ToArray();
		}

		/// <summary>
		/// Gets the booths that are not currently interpreting for any rooms.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<int> GetAvailableBoothIds()
		{
			return m_Booths.Keys.Where(boothId => m_Rooms.All(kvp => kvp.Value != boothId)).ToArray();
		}

		/// <summary>
		/// Begins forwarding 
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="boothId"></param>
		[PublicAPI]
		public void BeginInterpretation(int roomId, int boothId)
		{
			if (!m_Rooms.ContainsKey(roomId) || !m_Booths.ContainsKey(boothId))
				return;

			if (m_Rooms[roomId] != null)
				return;

			m_Rooms[roomId] = boothId;

			const string key = ConferencingClientDevice.SET_INTERPRETATION_STATE_RPC;
		}

		#endregion

		#region Private Helper Methods

		private void CallActionForConnectedClients(Action<uint> action)
		{
			if (m_Server == null)
				return;

			foreach (uint client in m_Server.GetClients())
				action(client);
		}

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

		private IEnumerable<IDialingDeviceControl> GetDialersForBooth(int boothId)
		{
			return m_Booths.ContainsKey(boothId)
				? m_Booths[boothId].GetDialers()
				: Enumerable.Empty<IDialingDeviceControl>();
		}

		private IEnumerable<IConferenceSource> GetConferenceSourcesForBooth(int boothId)
		{
			IDialingDeviceControl[] dialers = GetDialersForBooth(boothId).ToArray();

			if (dialers.Length == 0)
				return Enumerable.Empty<IConferenceSource>();

			List<IConferenceSource> sources = new List<IConferenceSource>();

			foreach (IDialingDeviceControl dialer in dialers)
			{
				sources.AddRange(dialer.GetSources());
			}
			return sources;
		}

		private bool TryGetTargetSource(int roomId, Guid id, out IConferenceSource targetSource)
		{
			targetSource = null;

			int? boothId;
			if (!m_Rooms.TryGetValue(roomId, out boothId))
			{
				Log(eSeverity.Error, "No room with id {0} exists.", roomId);
				return false;
			}

			if (boothId == null)
			{
				Log(eSeverity.Error, "No booth is assigned to room {0}", roomId);
				return false;
			}

			IEnumerable<IConferenceSource> sources = GetConferenceSourcesForBooth(boothId.Value);

			if (!m_Sources.TryGetValue(id, out targetSource))
			{
				Log(eSeverity.Error, "No Source with the given key found.");
				return false;
			}

			if (!sources.Contains(targetSource))
			{
				Log(eSeverity.Error, "Source {0} is not from the booth assigned to room {1}", id.ToString(), roomId);
				return false;
			}

			return true;
		}

		private bool TryGetClientIdForSource(Guid sourceId, out uint? clientId)
		{
			clientId = null;

			IConferenceSource source;
			if (!m_Sources.TryGetValue(sourceId, out source))
			{
				Log(eSeverity.Error, "No Source with the given key found.");
				return false;
			}

			int? targetBooth = null;
			foreach (var booth in m_Booths.Where(booth => booth.Value.GetDialers().Any(dialer => dialer.GetSources().Any(src => source == src))))
			{
				targetBooth = booth.Key;
				break;
			}
			if (targetBooth == null)
			{
				Log(eSeverity.Error, "Unable to match source {0} with an active booth.", sourceId);
				return false;
			}

			targetBooth = m_Booths.FirstOrDefault( booth => booth.Value.GetDialers().Any(dialer => dialer.GetSources().Any(src => source == src))).Key;


		}

		#endregion

		#region RPCs

		[Rpc(REGISTER_ROOM_RPC), UsedImplicitly]
		public void RegisterRoom(uint clientId, int roomId)
		{
			if (m_Clients.ContainsKey(clientId))
				return;

			if (m_Rooms.ContainsKey(roomId))
				return;

			m_Clients.Add(clientId, roomId);

			m_Rooms.Add(roomId, null);
		}

		[Rpc(UNREGISTER_ROOM_RPC), UsedImplicitly]
		public void UnregisterRoom(uint clientId, int roomId)
		{
			if (!m_Clients.ContainsKey(clientId))
				return;

			if (!m_Rooms.ContainsKey(roomId))
				return;

			m_Clients.Remove(clientId);

			m_Rooms.Remove(roomId);
		}

		[Rpc(PRIVACY_MUTE_RPC), UsedImplicitly]
		public void PrivacyMute(uint clientId, int roomId, bool enabled)
		{
			int? boothId;
			if (!m_Rooms.TryGetValue(roomId, out boothId))
				return;

			if (boothId == null)
				return;

			IEnumerable<IDialingDeviceControl> dialers = GetDialersForBooth(boothId.Value);
			foreach (IDialingDeviceControl dialer in dialers)
			{
				dialer.SetPrivacyMute(enabled);
			}
		}

		[Rpc(AUTO_ANSWER_RPC), UsedImplicitly]
		public void SetAutoAnswer(uint clientId, int roomId, bool enabled)
		{
			int? boothId;
			if (!m_Rooms.TryGetValue(roomId, out boothId))
				return;

			if (boothId == null)
				return;

			IEnumerable<IDialingDeviceControl> dialers = GetDialersForBooth(boothId.Value);
			foreach (IDialingDeviceControl dialer in dialers)
			{
				dialer.SetAutoAnswer(enabled);
			}
		}

		[Rpc(DO_NOT_DISTURB_RPC), UsedImplicitly]
		public void SetDoNotDisturb(uint clientId, int roomId, bool enabled)
		{
			int? boothId;
			if (!m_Rooms.TryGetValue(roomId, out boothId))
				return;

			if (boothId == null)
				return;

			IEnumerable<IDialingDeviceControl> dialers = GetDialersForBooth(boothId.Value);
			foreach (IDialingDeviceControl dialer in dialers)
			{
				dialer.SetDoNotDisturb(enabled);
			}
		}

		[Rpc(HOLD_ENABLE_RPC), UsedImplicitly]
		public void HoldEnable(uint clientId, int roomId, Guid id)
		{
			IConferenceSource targetSource;
			if (!TryGetTargetSource(roomId, id, out targetSource))
				return;

			targetSource.Hold();
		}

		[Rpc(ANSWER_RPC), UsedImplicitly]
		public void Answer(uint clientId, int roomId, Guid id)
		{
			IConferenceSource targetSource;
			if (!TryGetTargetSource(roomId, id, out targetSource))
				return;

			targetSource.Answer();
		}

		[Rpc(HOLD_RESUME_RPC), UsedImplicitly]
		public void HoldResume(uint clientId, int roomId, Guid id)
		{
			IConferenceSource targetSource;
			if (!TryGetTargetSource(roomId, id, out targetSource))
				return;

			targetSource.Resume();
		}

		[Rpc(END_CALL_RPC), UsedImplicitly]
		public void EndCall(uint clientId, int roomId, Guid id)
		{
			IConferenceSource targetSource;
			if (!TryGetTargetSource(roomId, id, out targetSource))
				return;

			targetSource.Hangup();
		}

		[Rpc(SEND_DTMF_RPC), UsedImplicitly]
		public void SendDtmf(uint clientId, int roomId, Guid id, string data)
		{
			IConferenceSource targetSource;
			if (!TryGetTargetSource(roomId, id, out targetSource))
				return;

			targetSource.SendDtmf(data);
		}
		#endregion

		#region Adapters

		private void AddAdapter(int boothId, IInterpretationAdapter adapter)
		{
			if (m_Adapters.ContainsValue(adapter))
				return;

			m_Adapters.Add(boothId, adapter);

			Subscribe(adapter);

			AddBoothForAdapter(boothId, adapter);
		}

		private void ClearAdapters()
		{
			foreach (IInterpretationAdapter adapter in m_Adapters.Values.ToArray())
				Unsubscribe(adapter);

			m_Adapters.Clear();

			ClearBooths();
		}

		private void Subscribe(IInterpretationAdapter adapter)
		{
			if (adapter == null)
				return;

			adapter.OnDialerAdded += AdapterOnDialerAdded;
			adapter.OnDialerRemoved += AdapterOnDialerRemoved;
		}

		private void Unsubscribe(IInterpretationAdapter adapter)
		{
			if (adapter == null)
				return;

			adapter.OnDialerAdded -= AdapterOnDialerAdded;
			adapter.OnDialerRemoved -= AdapterOnDialerRemoved;
		}

		private void AdapterOnDialerAdded(object sender, GenericEventArgs<IDialingDeviceControl> args)
		{
			IInterpretationAdapter adapter = sender as IInterpretationAdapter;
			if (adapter == null)
				return;

			int boothId;
			try
			{
				boothId = m_Adapters.GetKey(adapter);
			}
			catch (ArgumentOutOfRangeException ex)
			{
				Logger.AddEntry(eSeverity.Error, "Unable to find booth for adapter {0}", adapter.Id);
				return;
			}

			IInterpreterBooth booth = m_Booths[boothId];

			booth.AddDialer(args.Data);
		}

		private void AdapterOnDialerRemoved(object sender, GenericEventArgs<IDialingDeviceControl> args)
		{
			IInterpretationAdapter adapter = sender as IInterpretationAdapter;
			if (adapter == null)
				return;

			int boothId;
			try
			{
				boothId = m_Adapters.GetKey(adapter);
			}
			catch (ArgumentOutOfRangeException ex)
			{
				Logger.AddEntry(eSeverity.Error, "Unable to find booth for adapter {0}", adapter.Id);
				return;
			}

			IInterpreterBooth booth = m_Booths[boothId];

			booth.RemoveDialer(args.Data);
		}

		#endregion

		#region Booths

		private void AddBoothForAdapter(int boothId, IInterpretationAdapter adapter)
		{
			IInterpreterBooth booth = AddBooth(boothId);

			foreach (KeyValuePair<IDialingDeviceClientControl, string> keyValuePair in adapter.GetDialingControls())
			{
				booth.AddDialer(keyValuePair.Key, keyValuePair.Value);
			}
		}

		private IInterpreterBooth AddBooth(int boothId)
		{
			if (m_Booths.ContainsKey(boothId))
				return null;

			IInterpreterBooth booth = new InterpreterBooth(boothId);
			m_Booths.Add(boothId, booth);

			Subscribe(booth);

			return booth;
		}

		private void RemoveBooth(int boothId)
		{
			if (!m_Booths.ContainsKey(boothId))
				return;

			IInterpreterBooth booth = m_Booths[boothId];

			Unsubscribe(booth);

			m_Booths.Remove(boothId);
		}

		private void ClearBooths()
		{
			foreach (int booth in m_Booths.Keys)
				RemoveBooth(booth);
		}

		private void Subscribe(IInterpreterBooth booth)
		{
			if (booth == null)
				return;

			booth.OnDialerAdded += BoothOnDialerAdded;
			booth.OnDialerRemoved += BoothOnDialerRemoved;
		}

		private void Unsubscribe(IInterpreterBooth booth)
		{
			if (booth == null)
				return;

			booth.OnDialerAdded -= BoothOnDialerAdded;
			booth.OnDialerRemoved -= BoothOnDialerRemoved;
		}

		private void BoothOnDialerAdded(object sender, GenericEventArgs<IDialingDeviceControl> args)
		{
			Subscribe(args.Data);

			foreach (IConferenceSource source in args.Data.GetSources())
				AddSource(source);
		}

		private void BoothOnDialerRemoved(object sender, GenericEventArgs<IDialingDeviceControl> args)
		{
			Unsubscribe(args.Data);

			foreach (IConferenceSource source in args.Data.GetSources())
				RemoveSource(source);
		}

		#endregion

		#region Control

		private void Subscribe(IDialingDeviceControl control)
		{
			if (control == null)
				return;

			control.OnPrivacyMuteChanged += DialerOnPrivacyMuteChanged;
			control.OnAutoAnswerChanged += DialerOnAutoAnswerChanged;
			control.OnDoNotDisturbChanged += DialerOnDoNotDisturbChanged;
			control.OnSourceAdded += DialerOnSourceAdded;
		}

		private void Unsubscribe(IDialingDeviceControl control)
		{
			if (control == null)
				return;

			control.OnPrivacyMuteChanged -= DialerOnPrivacyMuteChanged;
			control.OnAutoAnswerChanged -= DialerOnAutoAnswerChanged;
			control.OnDoNotDisturbChanged -= DialerOnDoNotDisturbChanged;
			control.OnSourceAdded -= DialerOnSourceAdded;
		}

		private void DialerOnPrivacyMuteChanged(object sender, BoolEventArgs args)
		{
			const string key = ConferencingClientDevice.SET_CACHED_PRIVACY_MUTE_STATE;
			CallActionForConnectedClients(clientId => m_RpcController.CallMethod(clientId, key, args.Data));
		}

		private void DialerOnAutoAnswerChanged(object sender, BoolEventArgs args)
		{
			const string key = ConferencingClientDevice.SET_CACHED_AUTO_ANSWER_STATE;
			CallActionForConnectedClients(clientId => m_RpcController.CallMethod(clientId, key, args.Data));
		}

		private void DialerOnDoNotDisturbChanged(object sender, BoolEventArgs args)
		{
			const string key = ConferencingClientDevice.SET_CACHED_DO_NOT_DISTURB_STATE;
			CallActionForConnectedClients(clientId => m_RpcController.CallMethod(clientId, key, args.Data));
		}

		private void DialerOnSourceAdded(object sender, ConferenceSourceEventArgs args)
		{
			AddSource(args.Data);
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
				Log(eSeverity.Error, "Unknown source {0}", source.Name);
				return;
			}

			Guid id = m_Sources.GetKey(source);
			RpcConferenceSource rpcSource = new RpcConferenceSource(source);

			const string key = ConferencingClientDevice.UPDATE_CACHED_SOURCE_STATE;
			 m_RpcController.CallMethod(clientId, key, id, rpcSource);

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

			if (!m_Clients.ContainsKey(args.ClientId))
				return;

			int roomId = m_Clients[args.ClientId];
			UnregisterRoom(args.ClientId, roomId);
		}

		#endregion

		#region Settings

		protected override void ApplySettingsFinal(ConferencingServerDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			for (int boothId = 0; boothId < settings.WrappedDeviceIds.Count; boothId++)
			{
				int adapterId = settings.WrappedDeviceIds[boothId];
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

				AddAdapter(boothId, adapter);
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
			ClearBooths();
			ClearSources();
		}

		#endregion

		#region IDevice

		protected override bool GetIsOnlineStatus()
		{
			return m_Adapters.AnyAndAll(adapter => adapter.Value.IsOnline);
		}

		#endregion
	}
}