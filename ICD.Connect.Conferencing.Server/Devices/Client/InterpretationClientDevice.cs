using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Server.Devices.Server;
using ICD.Connect.Conferencing.Utils;
using ICD.Connect.Devices;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
    public sealed class InterpretationClientDevice : AbstractDevice<InterpretationClientDeviceSettings>, IClientInterpretationDevice
    {
	    #region Events

	    public event EventHandler OnInterpretationActiveChanged;

	    public event EventHandler<ParticipantEventArgs> OnSourceAdded;
	    public event EventHandler<ParticipantEventArgs> OnSourceRemoved;

	    public event EventHandler<BoolEventArgs> OnDoNotDisturbChanged;
	    public event EventHandler<BoolEventArgs> OnAutoAnswerChanged;
	    public event EventHandler<BoolEventArgs> OnPrivacyMuteChanged;

	    #endregion

		#region RPC Constants

	    public const string SET_INTERPRETATION_STATE_RPC = "SetInterpretationState";

	    public const string SET_CACHED_PRIVACY_MUTE_STATE = "SetCachedPrivacyMuteState";
		public const string SET_CACHED_AUTO_ANSWER_STATE = "SetCachedAutoAnswerState";
		public const string SET_CACHED_DO_NOT_DISTURB_STATE = "SetCachedDoNotDisturbState";

	    public const string UPDATE_CACHED_SOURCE_STATE = "UpdateCachedSourceState";
	    public const string REMOVE_CACHED_SOURCE = "RemoveCachedSource";

		#endregion

		#region Private Members
		
	    private readonly ClientSerialRpcController m_RpcController;
		private readonly Dictionary<Guid, ThinTraditionalParticipant> m_Sources;
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

			    if (m_IsConnected)
			    {
				    Log(eSeverity.Informational, "Connected To Server");
					Register();
			    }
			    else
			    {
				    Log(eSeverity.Alert, "Lost Connection To Server");
			    }
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
		    get
		    {
			    return m_PrivacyMuted;
		    }
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
		    get
		    {
			    return m_DoNotDisturb;
		    }
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
		    get
		    {
			    return m_AutoAnswer;
		    }
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
		    m_RpcController = new ClientSerialRpcController(this);
			m_Sources = new Dictionary<Guid, ThinTraditionalParticipant>();
			m_SourcesCriticalSection = new SafeCriticalSection();

			Controls.Add(new DialerDeviceDialerControl(this, 0));

			m_ConnectionStateManager = new ConnectionStateManager(this){ConfigurePort = ConfigurePort};
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectedStateChanged;
	    }

	    protected override void DisposeFinal(bool disposing)
	    {
		    OnSourceAdded = null;
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
		    {
			    RoomName = name;
		    }
	    }

	    public void SetRoomPrefixIfNullOrEmpty(string prefix)
	    {
		    // Only allow the room prefix to be set externally if an override isn't provided in settings.
		    if (string.IsNullOrEmpty(RoomPrefix))
		    {
			    RoomPrefix = prefix;
		    }
	    }

	    public void Register()
	    {
		    if(IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.REGISTER_ROOM_RPC, m_RoomId, RoomName, RoomPrefix);
	    }

	    public void Unregister()
	    {
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.UNREGISTER_ROOM_RPC, m_RoomId);
	    }

	    public void Dial(string number)
	    {
		    if(IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DIAL_RPC, m_RoomId, number);
	    }

	    public void Dial(string number, eCallType callType)
	    {
		    if(IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DIAL_TYPE_RPC, m_RoomId, number, callType);
	    }

		/// <summary>
		/// Returns the level of support the dialer has for the given booking.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public eDialContextSupport CanDial(IDialContext dialContext)
		{
			if(string.IsNullOrEmpty(dialContext.DialString))
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
		    if(IsConnected)
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
			m_SourcesCriticalSection.Enter();
			try
			{
				return m_Sources.Values.ToArray(m_Sources.Count);
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}
			
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

		private void ClearSources()
		{
			m_SourcesCriticalSection.Enter();
			try
			{
				foreach (ThinTraditionalParticipant src in m_Sources.Values)
				{
					src.Status = eParticipantStatus.Disconnected;
					Unsubscribe(src);
					IcdConsole.PrintLine(eConsoleColor.Magenta, "InterpretatonClientDevice-ClearSources-OnParticipantRemoved");
					OnSourceRemoved.Raise(this, new ParticipantEventArgs(src));
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

		    ClearSources();
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

	    [Rpc(REMOVE_CACHED_SOURCE), UsedImplicitly]
	    private void RemoveCachedSource(Guid id)
	    {
		    m_SourcesCriticalSection.Enter();

		    try
		    {
				if(!m_Sources.ContainsKey(id))
					return;

			    var sourceToRemove = m_Sources[id];
				sourceToRemove.Status = eParticipantStatus.Disconnected;
			    Unsubscribe(sourceToRemove);
			    m_Sources.Remove(id);

			    IcdConsole.PrintLine(eConsoleColor.Magenta, "InterpretatonClientDevice-RemoveCachedSource-OnParticipantRemoved");
			    OnSourceRemoved.Raise(this, new ParticipantEventArgs(sourceToRemove));
		    }
		    finally
		    {
			    m_SourcesCriticalSection.Leave();
		    }
	    }

	    [Rpc(UPDATE_CACHED_SOURCE_STATE), UsedImplicitly]
	    private void UpdateCachedSourceState(Guid id, ConferenceSourceState sourceState)
	    {
			m_SourcesCriticalSection.Enter();

		    try
		    {
			    bool added = false;

			    if (!m_Sources.ContainsKey(id))
			    {
					var newSrc = new ThinTraditionalParticipant();
				    m_Sources[id] = newSrc;
				    Subscribe(newSrc);

				    added = true;
			    }

			    var src = m_Sources[id];

			    src.Name = string.Format("({0}) {1}", sourceState.Language, sourceState.Name);
			    src.Number = sourceState.Number;
			    src.Status = sourceState.Status;
			    src.AnswerState = sourceState.AnswerState;
			    src.DialTime = sourceState.DialTime;
			    src.Direction = sourceState.Direction;
			    src.End = sourceState.End;
			    src.Start = sourceState.Start;
			    src.SourceType = sourceState.SourceType;

			    if (added)
			    {
					var control = Controls.GetControl<DialerDeviceDialerControl>();
				    if (control != null)
				    {
						IcdConsole.PrintLine(eConsoleColor.Magenta, "InterpretatonClientDevice-UpdateCachedSourceState-OnParticipantAdded");
					    OnSourceAdded.Raise(this, new ParticipantEventArgs(src));
				    }
			    }

			    if (sourceState.Status != eParticipantStatus.Disconnected)
				    return;

			    var sourceToRemove = m_Sources[id];
			    Unsubscribe(sourceToRemove);
			    m_Sources.Remove(id);

				IcdConsole.PrintLine(eConsoleColor.Magenta, "InterpretatonClientDevice-UpdateCachedSourceState-OnParticipantRemoved");
				OnSourceRemoved.Raise(this, new ParticipantEventArgs(sourceToRemove));
		    }
		    finally
		    {
			    m_SourcesCriticalSection.Leave();
		    }
		}

		#endregion

		#region Sources

		private void Subscribe(ThinTraditionalParticipant source)
		{
			source.AnswerCallback += SourceOnCallAnswered;
			source.HoldCallback += SourceOnCallHeld;
			source.ResumeCallback += SourceOnCallResumed;
			source.SendDtmfCallback += SourceOnDtmfSent;
			source.HangupCallback += SourceOnCallEnded;
	    }

		private void Unsubscribe(ThinTraditionalParticipant source)
	    {
			source.AnswerCallback = null;
			source.HoldCallback = null;
			source.ResumeCallback = null;
			source.SendDtmfCallback = null;
			source.HangupCallback = null;
		    
		}

		private void SourceOnCallAnswered(ThinTraditionalParticipant source)
		{
			if (source == null)
				return;
			
			m_SourcesCriticalSection.Enter();
			Guid id;
			try
			{
				if (!m_Sources.ContainsValue(source))
					return;

				id = m_Sources.GetKey(source);
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.ANSWER_RPC, m_RoomId, id);
		}

		private void SourceOnCallHeld(ThinTraditionalParticipant source)
	    {
			if (source == null)
			    return;

			m_SourcesCriticalSection.Enter();
			Guid id;
			try
			{
				if (!m_Sources.ContainsValue(source))
					return;

				id = m_Sources.GetKey(source);
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}

		    if (IsConnected)
			    m_RpcController.CallMethod(InterpretationServerDevice.HOLD_ENABLE_RPC, m_RoomId, id);
		}

		private void SourceOnCallResumed(ThinTraditionalParticipant source)
	    {
			if (source == null)
			    return;

			m_SourcesCriticalSection.Enter();
			Guid id;
			try
			{
				if (!m_Sources.ContainsValue(source))
					return;

				id = m_Sources.GetKey(source);
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}

		    if (IsConnected)
			    m_RpcController.CallMethod(InterpretationServerDevice.HOLD_RESUME_RPC, m_RoomId, id);
		}

		private void SourceOnCallEnded(ThinTraditionalParticipant source)
		{
			if (source == null)
				return;

			m_SourcesCriticalSection.Enter();
			Guid id;
			try
			{
				if (!m_Sources.ContainsValue(source))
					return;

				id = m_Sources.GetKey(source);
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}

			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.END_CALL_RPC, m_RoomId, id);
		}

		private void SourceOnDtmfSent(ThinTraditionalParticipant source, string data)
	    {
			if (source == null)
			    return;
			
			m_SourcesCriticalSection.Enter();
			Guid id;
			try
			{
				if (!m_Sources.ContainsValue(source))
					return;

				id = m_Sources.GetKey(source);
			}
			finally
			{
				m_SourcesCriticalSection.Leave();
			}

		    if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.SEND_DTMF_RPC, m_RoomId, id, data);
		}

		#endregion

		#region Port

		/// <summary>
		/// Sets the port for communication with the server.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
	    public void ConfigurePort(ISerialPort port)
	    {
			m_RpcController.SetPort(port);

		    UpdateCachedOnlineStatus();

			if(m_ConnectionStateManager.IsConnected)
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
		    ClearSources();
	    }

		#endregion

	    #region Settings

		protected override void ApplySettingsFinal(InterpretationClientDeviceSettings settings, IDeviceFactory factory)
	    {
		    base.ApplySettingsFinal(settings, factory);

			m_RoomId = settings.Room == null ? 0 : settings.Room.Value;

			RoomName = settings.RoomName;

			RoomPrefix = settings.RoomPrefix;

			ISerialPort port = null;

		    if (settings.Port != null)
			    port = factory.GetPortById(settings.Port.Value) as ISerialPort;

			if (port == null)
				Log(eSeverity.Error, "No Serial Port with id {0}", settings.Port);

			m_ConnectionStateManager.SetPort(port);
	    }

	    protected override void CopySettingsFinal(InterpretationClientDeviceSettings settings)
	    {
		    base.CopySettingsFinal(settings);

		    settings.Port = m_ConnectionStateManager.PortNumber;
		    settings.Room = m_RoomId;
	    }

	    protected override void ClearSettingsFinal()
	    {
		    base.ClearSettingsFinal();

			ConfigurePort(null);
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
			    addRow("State", src.AnswerState);
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

	    #endregion

		#region IDevice

		protected override bool GetIsOnlineStatus()
		{
			return m_ConnectionStateManager != null && m_ConnectionStateManager.IsOnline;
		}

	    #endregion
	}
}
