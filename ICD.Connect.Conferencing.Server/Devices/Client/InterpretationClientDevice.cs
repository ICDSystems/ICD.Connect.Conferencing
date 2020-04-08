using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Server.Devices.Server;
using ICD.Connect.Conferencing.Utils;
using ICD.Connect.Devices;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
	public sealed class InterpretationClientDevice : AbstractDevice<InterpretationClientDeviceSettings>,
													 IClientInterpretationDevice
	{
		#region Events

		public event EventHandler OnInterpretationActiveChanged;

		public event EventHandler<ParticipantEventArgs> OnParticipantAdded;
		public event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		public event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;
		public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;

		#endregion

		#region RPC Constants

		public const string SET_INTERPRETATION_STATE_RPC = "SetInterpretationState";

		public const string SET_CACHED_PRIVACY_MUTE_STATE = "SetCachedPrivacyMuteState";
		public const string SET_CACHED_AUTO_ANSWER_STATE = "SetCachedAutoAnswerState";
		public const string SET_CACHED_DO_NOT_DISTURB_STATE = "SetCachedDoNotDisturbState";

		public const string UPDATE_CACHED_PARTICIPANT_STATE = "UpdateCachedSourceState";
		public const string REMOVE_CACHED_PARTICIPANT = "RemoveCachedSource";

		#endregion

		#region Private Members

		private readonly SecureNetworkProperties m_NetworkProperties;

		private readonly ClientSerialRpcController m_RpcController;
		private readonly BiDictionary<Guid, ThinTraditionalParticipant> m_Sources;
		private readonly ConnectionStateManager m_ConnectionStateManager;
		private readonly SafeCriticalSection m_SourcesCriticalSection;

		private bool m_IsConnected;
		private bool m_IsInterpretationActive;
		private bool m_PrivacyMuted;
		private bool m_DoNotDisturb;
		private bool m_AutoAnswer;
		private int m_RoomId;

		#endregion

		#region Public Properties

		public bool IsConnected
		{
			get { return m_IsConnected; }
			private set
			{
				if (value == m_IsConnected)
					return;

				m_IsConnected = value;

				UpdateCachedOnlineStatus();

				eSeverity severity = m_IsConnected ? eSeverity.Informational : eSeverity.Alert;
				string message = m_IsConnected ? "Connected To Server" : "Lost Connection To Server";

				Logger.Set("Connected", severity, message);

				if (m_IsConnected)
					Register();
			}
		}

		public string RoomName { get; private set; }

		public string RoomPrefix { get; private set; }

		public bool IsInterpretationActive
		{
			get { return m_IsInterpretationActive; }
			private set
			{
				if (m_IsInterpretationActive == value)
					return;

				m_IsInterpretationActive = value;

				OnInterpretationActiveChanged.Raise(this);
			}
		}

		public bool PrivacyMuted
		{
			get { return m_PrivacyMuted; }
			private set
			{
				if (value == m_PrivacyMuted)
					return;

				m_PrivacyMuted = value;

				OnPrivacyMuteChanged.Raise(this, new BoolEventArgs(m_PrivacyMuted));
			}
		}

		public bool DoNotDisturb
		{
			get { return m_DoNotDisturb; }
			private set
			{
				if (value == m_DoNotDisturb)
					return;

				m_DoNotDisturb = value;

				OnDoNotDisturbChanged.Raise(this, new BoolEventArgs(m_DoNotDisturb));
			}
		}

		public bool AutoAnswer
		{
			get { return m_AutoAnswer; }
			private set
			{
				if (value == m_AutoAnswer)
					return;

				m_AutoAnswer = value;

				OnAutoAnswerChanged.Raise(this, new BoolEventArgs(m_AutoAnswer));
			}
		}

		#endregion

		public InterpretationClientDevice()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_RpcController = new ClientSerialRpcController(this);
			m_Sources = new BiDictionary<Guid, ThinTraditionalParticipant>();
			m_SourcesCriticalSection = new SafeCriticalSection();

			Controls.Add(new DialerDeviceDialerControl(this, 0));

			m_ConnectionStateManager = new ConnectionStateManager(this) { ConfigurePort = ConfigurePort };
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectedStateChanged;
		}

		protected override void DisposeFinal(bool disposing)
		{
			OnInterpretationActiveChanged = null;
			OnParticipantAdded = null;
			OnParticipantRemoved = null;
			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;
			OnDoNotDisturbChanged = null;
			OnAutoAnswerChanged = null;
			OnPrivacyMuteChanged = null;

			base.DisposeFinal(disposing);

			m_ConnectionStateManager.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnConnectedStateChanged -= PortOnConnectedStateChanged;
			m_ConnectionStateManager.Dispose();

			m_RpcController.Dispose();
		}

		#region	Public Methods

		public void SetRoomNameIfNullOrEmpty(string name)
		{
			// Only allow the room name to be set externally if an override isn't provided in settings.
			if (string.IsNullOrEmpty(RoomName))
				RoomName = name;
		}

		public void SetRoomPrefixIfNullOrEmpty(string prefix)
		{
			// Only allow the room prefix to be set externally if an override isn't provided in settings.
			if (string.IsNullOrEmpty(RoomPrefix))
				RoomPrefix = prefix;
		}

		public void Register()
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.REGISTER_ROOM_RPC, m_RoomId, RoomName, RoomPrefix);
		}

		public void Unregister()
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.UNREGISTER_ROOM_RPC, m_RoomId);
		}

		public void Dial(string number)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DIAL_RPC, m_RoomId, number);
		}

		public void Dial(string number, eCallType callType)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DIAL_TYPE_RPC, m_RoomId, number, callType);
		}

		/// <summary>
		/// Returns the level of support the dialer has for the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public eDialContextSupport CanDial(IDialContext dialContext)
		{
			if (string.IsNullOrEmpty(dialContext.DialString))
				return eDialContextSupport.Unsupported;

			if (dialContext.Protocol == eDialProtocol.Sip && SipUtils.IsValidSipUri(dialContext.DialString))
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == eDialProtocol.Pstn)
				return eDialContextSupport.Supported;

			return eDialContextSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		public void Dial(IDialContext dialContext)
		{
			Dial(dialContext.DialString);
		}

		public void SetPrivacyMute(bool enabled)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.PRIVACY_MUTE_RPC, m_RoomId, enabled);
		}

		public void SetAutoAnswer(bool enabled)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.AUTO_ANSWER_RPC, m_RoomId, enabled);
		}

		public void SetDoNotDisturb(bool enabled)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DO_NOT_DISTURB_RPC, m_RoomId, enabled);
		}

		[PublicAPI]
		public IEnumerable<ITraditionalParticipant> GetSources()
		{
			return m_SourcesCriticalSection.Execute(() => m_Sources.Values.ToArray(m_Sources.Count));
		}

		#endregion

		#region Private Helper Methods

		private void ClearParticipants()
		{
			m_SourcesCriticalSection.Enter();
			try
			{
				foreach (ThinTraditionalParticipant src in m_Sources.Values)
				{
					src.SetStatus(eParticipantStatus.Disconnected);
					Unsubscribe(src);

					OnParticipantRemoved.Raise(this, new ParticipantEventArgs(src));
				}

				m_Sources.Clear();
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}
		}

		#endregion

		#region RPCs

		[Rpc(SET_INTERPRETATION_STATE_RPC), UsedImplicitly]
		private void SetInterpretationState(bool state)
		{
			IsInterpretationActive = state;

			ClearParticipants();
		}

		[Rpc(SET_CACHED_PRIVACY_MUTE_STATE), UsedImplicitly]
		private void SetCachedPrivacyMuteState(bool state)
		{
			PrivacyMuted = state;
		}

		[Rpc(SET_CACHED_AUTO_ANSWER_STATE), UsedImplicitly]
		private void SetCachedAutoAnswerState(bool state)
		{
			AutoAnswer = state;
		}

		[Rpc(SET_CACHED_DO_NOT_DISTURB_STATE), UsedImplicitly]
		private void SetCachedDoNotDisturbState(bool state)
		{
			DoNotDisturb = state;
		}

		[Rpc(REMOVE_CACHED_PARTICIPANT), UsedImplicitly]
		private void RemoveCachedSource(Guid id)
		{
			m_SourcesCriticalSection.Enter();

			try
			{
				if (!m_Sources.ContainsKey(id))
					return;

				var sourceToRemove = m_Sources.GetValue(id);
				sourceToRemove.SetStatus(eParticipantStatus.Disconnected);
				Unsubscribe(sourceToRemove);
				m_Sources.RemoveKey(id);

				OnParticipantRemoved.Raise(this, new ParticipantEventArgs(sourceToRemove));
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}
		}

		[Rpc(UPDATE_CACHED_PARTICIPANT_STATE), UsedImplicitly]
		private void UpdateCachedSourceState(Guid id, ParticipantState participantState)
		{
			m_SourcesCriticalSection.Enter();

			try
			{
				bool added = false;

				if (!m_Sources.ContainsKey(id))
				{
					var newSrc = new ThinTraditionalParticipant();
					m_Sources.Set(id, newSrc);
					Subscribe(newSrc);

					added = true;
				}

				var src = m_Sources.GetValue(id);

				src.SetName(string.Format("({0}) {1}", participantState.Language, participantState.Name));
				src.SetNumber(participantState.Number);
				src.SetStatus(participantState.Status);
				src.SetDialTime(participantState.DialTime);
				src.SetDirection(participantState.Direction);
				if (participantState.End != null)
					src.SetEnd((DateTime)participantState.End);
				if (participantState.Start != null)
					src.SetStart((DateTime)participantState.Start);
				src.SetCallType(participantState.SourceType);

				if (added)
				{
					var control = Controls.GetControl<DialerDeviceDialerControl>();
					if (control != null)
						OnParticipantAdded.Raise(this, new ParticipantEventArgs(src));
				}

				if (participantState.Status != eParticipantStatus.Disconnected)
					return;

				var sourceToRemove = m_Sources.GetValue(id);
				Unsubscribe(sourceToRemove);
				m_Sources.RemoveKey(id);

				OnParticipantRemoved.Raise(this, new ParticipantEventArgs(sourceToRemove));
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}
		}

		#endregion

		#region Participants

		private void Subscribe(ThinTraditionalParticipant participant)
		{
			participant.HoldCallback += ParticipantOnCallHeld;
			participant.ResumeCallback += ParticipantOnCallResumed;
			participant.SendDtmfCallback += ParticipantOnDtmfSent;
			participant.HangupCallback += ParticipantOnCallEnded;
		}

		private void Unsubscribe(ThinTraditionalParticipant participant)
		{
			participant.HoldCallback = null;
			participant.ResumeCallback = null;
			participant.SendDtmfCallback = null;
			participant.HangupCallback = null;
		}

		private void ParticipantOnCallAnswered(ThinTraditionalParticipant source)
		{
			if (source == null)
				return;

			Guid id;
			if (!TryGetId(source, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.ANSWER_RPC, m_RoomId, id);
		}

		private void ParticipantOnCallHeld(ThinTraditionalParticipant source)
		{
			if (source == null)
				return;

			Guid id;
			if (!TryGetId(source, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.HOLD_ENABLE_RPC, m_RoomId, id);
		}

		private void ParticipantOnCallResumed(ThinTraditionalParticipant source)
		{
			if (source == null)
				return;

			Guid id;
			if (!TryGetId(source, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.HOLD_RESUME_RPC, m_RoomId, id);
		}

		private void ParticipantOnCallEnded(ThinTraditionalParticipant source)
		{
			if (source == null)
				return;

			Guid id;
			if (!TryGetId(source, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.END_CALL_RPC, m_RoomId, id);
		}

		private void ParticipantOnDtmfSent(ThinTraditionalParticipant source, string data)
		{
			if (source == null)
				return;

			Guid id;
			if (!TryGetId(source, out id))
				return;

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.SEND_DTMF_RPC, m_RoomId, id, data);
		}

		private bool TryGetId(ThinTraditionalParticipant participant, out Guid id)
		{
			m_SourcesCriticalSection.Enter();

			try
			{
				return m_Sources.TryGetKey(participant, out id);
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}
		}

		#endregion

		#region Port

		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			m_ConnectionStateManager.SetPort(port);
		}

		/// <summary>
		/// Sets the port for communication with the server.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void ConfigurePort(ISerialPort port)
		{
			// Network (TCP, UDP, SSH)
			if (port is ISecureNetworkPort)
				(port as ISecureNetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
			else if (port is INetworkPort)
				(port as INetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);

			m_RpcController.SetPort(port);

			UpdateCachedOnlineStatus();

			if (m_ConnectionStateManager.IsConnected)
				Register();
		}

		/// <summary>
		/// Called when the port online status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Called when the port connection state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectedStateChanged(object sender, BoolEventArgs args)
		{
			IsConnected = m_ConnectionStateManager != null && m_ConnectionStateManager.IsConnected;

			if (IsConnected)
				return;

			IsInterpretationActive = false;
			ClearParticipants();
		}

		#endregion

		#region Settings

		protected override void ApplySettingsFinal(InterpretationClientDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_RoomId = settings.Room == null ? 0 : settings.Room.Value;

			RoomName = settings.RoomName;
			RoomPrefix = settings.RoomPrefix;

			m_NetworkProperties.Copy(settings);

			ISerialPort port = null;

			if (settings.Port != null)
			{
				try
				{
					port = factory.GetPortById(settings.Port.Value) as ISerialPort;
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No Serial Port with id {0}", settings.Port);
				}
			}

			SetPort(port);
		}

		protected override void CopySettingsFinal(InterpretationClientDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_ConnectionStateManager.PortNumber;
			settings.Room = m_RoomId;
			settings.RoomName = RoomName;
			settings.RoomPrefix = RoomPrefix;

			settings.Copy(m_NetworkProperties);
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_NetworkProperties.ClearNetworkProperties();

			SetPort(null);
		}

		#endregion

		#region Console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			var sources = GetSources();
			addRow("Interpretation Active", m_IsInterpretationActive);
			addRow("Remote Sources", "Count: " + sources.Count());
			foreach (var src in GetSources())
			{
				addRow("-----", "-----");
				addRow("Name", src.Name);
				addRow("Number", src.Number);
				addRow("Status", src.Status);
				addRow("Start", src.GetStartOrDialTime());
			}
			addRow("-----", "-----");
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsolCommands())
				yield return command;

			yield return new ConsoleCommand("Connect", "Connect to the server", () => m_ConnectionStateManager.Connect());
			yield return new ConsoleCommand("Disconnect", "Disconnect from the server", () => m_ConnectionStateManager.Disconnect());
			yield return new ConsoleCommand("Register", "Register the room with the server", () => Register());
			yield return new ConsoleCommand("Unregister", "Unregister the room with the server", () => Unregister());
		}

		private IEnumerable<IConsoleCommand> GetBaseConsolCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			if (m_ConnectionStateManager != null)
				yield return m_ConnectionStateManager.Port;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion

		#region IDevice

		protected override bool GetIsOnlineStatus()
		{
			return m_ConnectionStateManager != null && m_ConnectionStateManager.IsOnline;
		}

		#endregion
	}
}
