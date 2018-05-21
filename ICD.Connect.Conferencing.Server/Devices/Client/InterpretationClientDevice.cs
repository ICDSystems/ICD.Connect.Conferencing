using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Server.Devices.Simpl.Server;
using ICD.Connect.Devices;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Heartbeat;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
    public sealed class InterpretationClientDevice : AbstractDevice<InterpretationClientDeviceSettings>, IClientInterpretationDevice, IConnectable
    {
	    #region Events

	    public event EventHandler OnInterpretationActiveChanged;

		[PublicAPI]
	    public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

	    public event EventHandler<ConferenceSourceEventArgs> OnSourceAdded;
	    public event EventHandler<ConferenceSourceEventArgs> OnSourceRemoved;

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

		#endregion

		#region Private Members

		private bool m_IsConnected;
	    private ISerialPort m_Port;
	    private readonly ClientSerialRpcController m_RpcController;

	    private readonly Dictionary<Guid, ThinConferenceSource> m_Sources;

	    private readonly SafeCriticalSection m_SourcesCriticalSection;
	    private bool m_PrivacyMuted;
	    private bool m_DoNotDisturb;
	    private bool m_AutoAnswer;
	    private int m_Room;
	    private bool m_IsInterpretationActive;

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
			    }
			    else
			    {
				    Log(eSeverity.Alert, "Lost Connection To Server");
			    }

			    OnConnectedStateChanged.Raise(this, new BoolEventArgs(m_IsConnected));
		    }

	    }

		public Heartbeat Heartbeat { get; private set; }

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
			m_Sources = new Dictionary<Guid, ThinConferenceSource>();
			m_SourcesCriticalSection = new SafeCriticalSection();

			Controls.Add(new DialerDeviceDialerControl(this, 0));

			Heartbeat = new Heartbeat(this);
	    }

	    protected override void DisposeFinal(bool disposing)
	    {
		    OnConnectedStateChanged = null;
		    OnSourceAdded = null;
		    OnDoNotDisturbChanged = null;
		    OnAutoAnswerChanged = null;
		    OnPrivacyMuteChanged = null;

		    base.DisposeFinal(disposing);

			Heartbeat.Dispose();

			SetPort(null);
		    m_RpcController.Dispose();
	    }

		#region	Public Methods

	    public void Register()
	    {
		    if(IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.REGISTER_ROOM_RPC, m_Room);
	    }

	    public void Unregister()
	    {
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.UNREGISTER_ROOM_RPC, m_Room);
	    }

	    public void Dial(string number)
	    {
		    if(IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DIAL_RPC, m_Room, number);
	    }

	    public void Dial(string number, eConferenceSourceType callType)
	    {
		    if(IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DIAL_TYPE_RPC, m_Room, number, callType);
	    }

		public void SetPrivacyMute(bool enabled)
	    {
		    if(IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.PRIVACY_MUTE_RPC, m_Room, enabled);
	    }

		public void SetAutoAnswer(bool enabled)
		{
			if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.AUTO_ANSWER_RPC, m_Room, enabled);
		}

	    public void SetDoNotDisturb(bool enabled)
	    {
		    if (IsConnected)
				m_RpcController.CallMethod(InterpretationServerDevice.DO_NOT_DISTURB_RPC, m_Room, enabled);
	    }

		[PublicAPI]
	    public IEnumerable<IConferenceSource> GetSources()
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

		/// <summary>
		/// Connect the instance to the remote endpoint.
		/// </summary>
		public void Connect()
		{
			if (m_Port != null && !m_Port.IsConnected)
				m_Port.Connect();

			Register();
		}

		/// <summary>
		/// Disconnects the instance from the remote endpoint.
		/// </summary>
		public void Disconnect()
		{
			Unregister();

			if (m_Port != null && m_Port.IsConnected)
				m_Port.Disconnect();
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
				foreach (var src in m_Sources.Values)
					Unsubscribe(src);

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

	    [Rpc(UPDATE_CACHED_SOURCE_STATE), UsedImplicitly]
	    private void UpdateCachedSourceState(Guid id, ConferenceSourceState sourceState)
	    {
			m_SourcesCriticalSection.Enter();

		    try
		    {
			    bool added = false;

			    if (!m_Sources.ContainsKey(id))
			    {
				    var newSrc = new ThinConferenceSource();
				    m_Sources[id] = newSrc;
				    Subscribe(newSrc);

				    added = true;
			    }

			    var src = m_Sources[id];

			    src.Name = sourceState.Name;
			    src.Number = sourceState.Number;
			    src.Status = sourceState.Status;
			    src.AnswerState = sourceState.AnswerState;
			    src.DialTime = sourceState.DialTime;
			    src.Direction = sourceState.Direction;
			    src.End = sourceState.End;
			    src.Start = sourceState.Start;

			    if (added)
			    {
					var control = Controls.GetControl<DialerDeviceDialerControl>();
				    if (control != null)
						OnSourceAdded.Raise(this, new ConferenceSourceEventArgs(src));
			    }

			    if (sourceState.Status != eConferenceSourceStatus.Disconnected)
				    return;

			    var sourceToRemove = m_Sources[id];
			    Unsubscribe(sourceToRemove);
			    m_Sources.Remove(id);

				OnSourceRemoved.Raise(this, new ConferenceSourceEventArgs(sourceToRemove));
		    }
		    finally
		    {
			    m_SourcesCriticalSection.Leave();
		    }
		}

		#endregion

		#region Sources

		private void Subscribe(ThinConferenceSource source)
		{
			source.AnswerCallback += SourceOnCallAnswered;
			source.HoldCallback += SourceOnCallHeld;
			source.ResumeCallback += SourceOnCallResumed;
			source.SendDtmfCallback += SourceOnDtmfSent;
			source.HangupCallback += SourceOnCallEnded;
	    }
		
	    private void Unsubscribe(ThinConferenceSource source)
	    {
			source.AnswerCallback = null;
			source.HoldCallback = null;
			source.ResumeCallback = null;
			source.SendDtmfCallback = null;
			source.HangupCallback = null;
		    
		}

		private void SourceOnCallAnswered(ThinConferenceSource source)
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
				m_RpcController.CallMethod(InterpretationServerDevice.ANSWER_RPC, id);
		}

		private void SourceOnCallHeld(ThinConferenceSource source)
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
			    m_RpcController.CallMethod(InterpretationServerDevice.HOLD_ENABLE_RPC, id);
		}

		private void SourceOnCallResumed(ThinConferenceSource source)
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
			    m_RpcController.CallMethod(InterpretationServerDevice.HOLD_RESUME_RPC, id);
		}

		private void SourceOnCallEnded(ThinConferenceSource source)
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
				m_RpcController.CallMethod(InterpretationServerDevice.END_CALL_RPC, id);
		}

		private void SourceOnDtmfSent(ThinConferenceSource source, string data)
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
			    m_RpcController.CallMethod(InterpretationServerDevice.SEND_DTMF_RPC, id, data);
		}

		#endregion

		#region Port

		/// <summary>
		/// Sets the port for communication with the server.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
	    public void SetPort(ISerialPort port)
	    {
		    if (port == m_Port)
			    return;

		    Unsubscribe(m_Port);

		    m_Port = port;
		    m_RpcController.SetPort(m_Port);

		    Subscribe(m_Port);

			m_Port.Connect();

		    UpdateCachedOnlineStatus();
	    }

	    /// <summary>
	    /// Subscribe to the port events.
	    /// </summary>
	    /// <param name="port"></param>
	    private void Subscribe(ISerialPort port)
	    {
		    if (port == null)
			    return;

		    port.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
		    port.OnConnectedStateChanged += PortOnConnectedStateChanged;
	    }

	    /// <summary>
	    /// Unsubscribe from the port events.
	    /// </summary>
	    /// <param name="port"></param>
	    private void Unsubscribe(ISerialPort port)
	    {
		    if (port == null)
			    return;

		    port.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
		    port.OnConnectedStateChanged += PortOnConnectedStateChanged;
	    }

	    /// <summary>
	    /// Called when the port online status changes.
	    /// </summary>
	    /// <param name="sender"></param>
	    /// <param name="args"></param>
	    private void PortOnIsOnlineStateChanged(object sender, DeviceBaseOnlineStateApiEventArgs args)
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
		    IsConnected = m_Port != null && m_Port.IsConnected;
	    }

		#endregion

	    #region Settings

		protected override void ApplySettingsFinal(InterpretationClientDeviceSettings settings, IDeviceFactory factory)
	    {
		    base.ApplySettingsFinal(settings, factory);

			ISerialPort port = null;

		    if (settings.Port != null)
			    port = factory.GetPortById(settings.Port.Value) as ISerialPort;

			if (port == null)
				Log(eSeverity.Error, "No Serial Port with id {0}", settings.Port);

			SetPort(port);

			m_Room = settings.Room == null ? 0 : settings.Room.Value;

			Heartbeat.StartMonitoring();
	    }

	    protected override void CopySettingsFinal(InterpretationClientDeviceSettings settings)
	    {
		    base.CopySettingsFinal(settings);

		    settings.Port = m_Port == null ? (int?)null : m_Port.Id;
		    settings.Room = m_Room;
	    }

	    protected override void ClearSettingsFinal()
	    {
		    base.ClearSettingsFinal();

			SetPort(null);

			Heartbeat.StopMonitoring();
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
		    addRow("Remote Sources", "Count: " + sources.Count());
		    foreach (var src in GetSources())
		    {
				addRow("-----", "-----");
			    addRow("Name", src.Name);
			    addRow("Number", src.Number);
			    addRow("Status", src.Status);
			    addRow("State", src.AnswerState);
			    addRow("Start", src.StartOrDialTime);
		    }
			addRow("-----", "-----");
	    }

	    #endregion

		#region IDevice

		protected override bool GetIsOnlineStatus()
	    {
		    return m_Port != null && m_Port.IsOnline;
	    }

	    #endregion
	}
}
