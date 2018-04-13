using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Attributes.Rpc;
using ICD.Connect.Protocol.Network.RemoteProcedure;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Server
{
    public sealed class ConferencingClientDevice : AbstractDevice<ConferencingClientDeviceSettings>, IConferencingClientDevice
    {
	    #region Events
		[PublicAPI]
	    public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

	    #endregion

		#region RPC Constants

	    public const string SET_CACHED_PRIVACY_MUTE_STATE = "SetCachedPrivacyMuteState";
	    public const string UPDATE_CACHED_SOURCE_STATE = "UpdateCachedSourceState";

		#endregion

		#region Private Members

		private bool m_IsConnected;
	    private ISerialPort m_Port;
	    private readonly ClientSerialRpcController m_RpcController;

	    private bool m_PrivacyMuteEnabled;
	    private readonly Dictionary<Guid, ThinConferenceSource> m_Sources;

	    private readonly SafeCriticalSection m_SourcesCriticalSection;

		#endregion

	    #region Public Properties

	    [PublicAPI]
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

		[PublicAPI]
		public bool PrivacyMuteEnabled { get { return m_PrivacyMuteEnabled; } }

		[PublicAPI]
		public bool HoldEnabled
		{
			get { return m_Sources.All(source => source.Value.Status == eConferenceSourceStatus.OnHold); } 
		}

		[PublicAPI]
	    public bool CallEnded
	    {
			get { return m_Sources.All(source => source.Value.Status == eConferenceSourceStatus.Disconnected); }
	    }

	    #endregion

		public ConferencingClientDevice()
	    {
		    m_RpcController = new ClientSerialRpcController(this);
			m_Sources = new Dictionary<Guid, ThinConferenceSource>();

			Controls.Add(new DialingDeviceClientControl(this, 0));

			m_SourcesCriticalSection = new SafeCriticalSection();
	    }

	    protected override void DisposeFinal(bool disposing)
	    {
		    OnConnectedStateChanged = null;

		    base.DisposeFinal(disposing);

			SetPort(null);
		    m_RpcController.Dispose();
	    }

		#region	Public Methods

	    [PublicAPI]
	    public void Dial(string number)
	    {
		    if (IsConnected)
			    m_RpcController.CallMethod(ConferencingServerDevice.DIAL_RPC, number);
	    }

	    [PublicAPI]
	    public void Dial(string number, eConferenceSourceType type)
	    {
		    if (IsConnected)
			    m_RpcController.CallMethod(ConferencingServerDevice.DIAL_TYPE_RPC, number, type);
	    }

		[PublicAPI]
	    public void SetPrivacyMute(bool enabled)
	    {
		    if(IsConnected)
				m_RpcController.CallMethod(ConferencingServerDevice.PRIVACY_MUTE_RPC, enabled);
	    }

		[PublicAPI]
		public void SetAutoAnswer(bool enabled)
		{
			if (IsConnected)
				m_RpcController.CallMethod(ConferencingServerDevice.AUTO_ANSWER_RPC, enabled);
		}

	    [PublicAPI]
	    public void SetDoNotDisturb(bool enabled)
	    {
		    if (IsConnected)
			    m_RpcController.CallMethod(ConferencingServerDevice.DO_NOT_DISTURB_RPC, enabled);
	    }

		[PublicAPI]
		public void HoldEnable()
	    {
			if (IsConnected)
				m_RpcController.CallMethod(ConferencingServerDevice.HOLD_ENABLE_RPC);
		}

		[PublicAPI]
		public void HoldResume()
	    {
			if (IsConnected)
				m_RpcController.CallMethod(ConferencingServerDevice.HOLD_RESUME_RPC);
		}

		[PublicAPI]
		public void EndCall()
	    {
			if (IsConnected)
				m_RpcController.CallMethod(ConferencingServerDevice.END_CALL_RPC);
		}

		[PublicAPI]
		public void SendDtmf(string data)
	    {
			if (IsConnected)
				m_RpcController.CallMethod(ConferencingServerDevice.SEND_DTMF_RPC, data);
		}

		[PublicAPI]
	    public IEnumerable<IConferenceSource> GetSources()
		{
			return m_Sources.Values;
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

		#endregion

	    #region RPCs

	    [Rpc(SET_CACHED_PRIVACY_MUTE_STATE), UsedImplicitly]
	    private void SetCachedPrivacyMuteState(bool state)
	    {
		    m_PrivacyMuteEnabled = state;
	    }

	    [Rpc(UPDATE_CACHED_SOURCE_STATE), UsedImplicitly]
	    private void UpdateCachedSourceState(Guid id, RpcConferenceSource source)
	    {
			m_SourcesCriticalSection.Enter();
		    try
		    {
			    if (!m_Sources.ContainsKey(id))
			    {
				    var newSrc = new ThinConferenceSource
				    {
					    Name = source.Name,
					    Number = source.Number,
					    Status = source.Status,
					    AnswerState = source.AnswerState,
					    DialTime = source.DialTime,
					    Direction = source.Direction,
					    End = source.End,
					    Start = source.Start
				    };
				    m_Sources[id] = newSrc;

					Subscribe(newSrc);

				    var control = Controls.GetControl<IDialingDeviceClientControl>();
				    if (control != null)
					    control.RaiseSourceAdded(newSrc);
			    }
			    else
			    {
				    var src = m_Sources[id];

				    src.Name = source.Name;
				    src.Number = source.Number;
				    src.Status = source.Status;
				    src.AnswerState = source.AnswerState;
				    src.DialTime = source.DialTime;
				    src.Direction = source.Direction;
				    src.End = source.End;
				    src.Start = source.Start;
			    }

			    if (source.Status != eConferenceSourceStatus.Disconnected)
				    return;

			    var sourceToRemove = m_Sources[id];
				Unsubscribe(sourceToRemove);
			    m_Sources.Remove(id);
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
			source.OnAnswerCallback += SourceOnAnswerCallback;
			source.OnHoldCallback += SourceOnHoldCallback;
			source.OnResumeCallback += SourceOnResumeCallback;
			source.OnHangupCallback += SourceOnHangupCallback;
			source.OnSendDtmfCallback += SourceOnSendDtmfCallback;
	    }
		
	    private void Unsubscribe(ThinConferenceSource source)
	    {
		    source.OnAnswerCallback -= SourceOnAnswerCallback;
		    source.OnHoldCallback -= SourceOnHoldCallback;
		    source.OnResumeCallback -= SourceOnResumeCallback;
		    source.OnHangupCallback -= SourceOnHangupCallback;
		    source.OnSendDtmfCallback -= SourceOnSendDtmfCallback;
		}

		private void SourceOnAnswerCallback(object sender, EventArgs eventArgs)
		{
			var source = sender as ThinConferenceSource;
			if (source == null)
				return;

			if(!m_Sources.ContainsValue(source))
				return;

			var id = m_Sources.GetKey(source);

			if(IsConnected)
				m_RpcController.CallMethod(ConferencingServerDevice.ANSWER_RPC, id);
		}

	    private void SourceOnHoldCallback(object sender, EventArgs eventArgs)
	    {
			var source = sender as ThinConferenceSource;
		    if (source == null)
			    return;

		    if (!m_Sources.ContainsValue(source))
			    return;

		    var id = m_Sources.GetKey(source);

		    if (IsConnected)
			    m_RpcController.CallMethod(ConferencingServerDevice.HOLD_ENABLE_RPC, id);
		}

	    private void SourceOnResumeCallback(object sender, EventArgs eventArgs)
	    {
			var source = sender as ThinConferenceSource;
		    if (source == null)
			    return;

		    if (!m_Sources.ContainsValue(source))
			    return;

		    var id = m_Sources.GetKey(source);

		    if (IsConnected)
			    m_RpcController.CallMethod(ConferencingServerDevice.HOLD_RESUME_RPC, id);
		}

		private void SourceOnHangupCallback(object sender, EventArgs eventArgs)
		{
			var source = sender as ThinConferenceSource;
			if (source == null)
				return;

			if (!m_Sources.ContainsValue(source))
				return;

			var id = m_Sources.GetKey(source);

			if (IsConnected)
				m_RpcController.CallMethod(ConferencingServerDevice.END_CALL_RPC, id);
		}

		private void SourceOnSendDtmfCallback(object sender, StringEventArgs args)
	    {
			var source = sender as ThinConferenceSource;
		    if (source == null)
			    return;

		    if (!m_Sources.ContainsValue(source))
			    return;

		    var id = m_Sources.GetKey(source);

		    if (IsConnected)
			    m_RpcController.CallMethod(ConferencingServerDevice.SEND_DTMF_RPC, id, args.Data);
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

		    IsConnected = m_Port != null && m_Port.IsConnected;

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
		    IsConnected = args.Data;
	    }
		#endregion

	    #region Settings
		protected override void ApplySettingsFinal(ConferencingClientDeviceSettings settings, IDeviceFactory factory)
	    {
		    base.ApplySettingsFinal(settings, factory);

			ISerialPort port = null;

		    if (settings.Port != null)
			    port = factory.GetPortById(settings.Port.Value) as ISerialPort;

			if(port == null)
				Log(eSeverity.Error, "No Serial Port with id {0}", settings.Port);

			SetPort(port);
	    }

	    protected override void CopySettingsFinal(ConferencingClientDeviceSettings settings)
	    {
		    base.CopySettingsFinal(settings);

		    settings.Port = m_Port == null ? (int?)null : m_Port.Id;
	    }

	    protected override void ClearSettingsFinal()
	    {
		    base.ClearSettingsFinal();

			SetPort(null);
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
