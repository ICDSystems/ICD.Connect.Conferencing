using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Controls;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Controls.Conferencing;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Data;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialQueues;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge
{
	public sealed class VaddioAvBridgeDevice : AbstractVideoConferenceDevice<VaddioAvBridgeDeviceSettings>
	{
		/// <summary>
		/// End of line string.
		/// </summary>
		private const string END_OF_LINE = "\r\n";

		/// <summary>
		/// Status code of a response message with no errors.
		/// </summary>
		private const string SUCCESS_STATUS_CODE = "OK";

		#region Events

		/// <summary>
		/// Raised when the class initializes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		/// <summary>
		/// Raised when the device becomes connected or disconnected.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		#endregion

		private readonly Dictionary<string, IcdHashSet<Action<VaddioAvBridgeSerialResponse>>> m_FeedbackHandlers;

		private readonly SecureNetworkProperties m_NetworkProperties;
		private readonly ComSpecProperties m_ComSpecProperties;
		private readonly ConnectionStateManager m_ConnectionStateManager;

		private readonly VaddioAvBridgeComponentFactory m_Components;

		private readonly VaddioAvBridgeSerialBuffer m_SerialBuffer;
		private readonly SerialQueue m_SerialQueue;

		private bool m_Initialized;

		#region Properties

		/// <summary>
		/// Username for loggin into the device
		/// </summary>
		[PublicAPI]
		public string Username { get; set; }

		/// <summary>
		/// Password for logging in to the device.
		/// </summary>
		[PublicAPI]
		public string Password { get; set; }

		/// <summary>
		/// Device Initialized Status.
		/// </summary>
		public bool Initialized
		{
			get { return m_Initialized; }
			private set
			{
				if (value == m_Initialized)
					return;

				m_Initialized = value;

				OnInitializedChanged.Raise(this, new BoolEventArgs(m_Initialized));
			}
		}

		/// <summary>
		/// Provides the components attached to this AV Bridge.
		/// </summary>
		public VaddioAvBridgeComponentFactory Components { get { return m_Components; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp {get { return "The Vaddio AV Bridge device"; }}

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		public VaddioAvBridgeDevice()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_ComSpecProperties = new ComSpecProperties();

			m_FeedbackHandlers = new Dictionary<string, IcdHashSet<Action<VaddioAvBridgeSerialResponse>>>();

			m_SerialBuffer = new VaddioAvBridgeSerialBuffer();
			Subscribe(m_SerialBuffer);

			m_SerialQueue = new SerialQueue();
			m_SerialQueue.SetBuffer(m_SerialBuffer);
			Subscribe(m_SerialQueue);

			m_ConnectionStateManager = new ConnectionStateManager(this) { ConfigurePort = ConfigurePort };
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;

			m_Components = new VaddioAvBridgeComponentFactory(this);
		}

		protected override void AddControls(VaddioAvBridgeDeviceSettings settings, IDeviceFactory factory, Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new VaddioAvBridgeRoutingControl(this, 0));
			addControl(new VaddioAvBridgeVolumeControl(this, 1));
			addControl(new VaddioAvBridgeConferenceControl(this, 2));
			addControl(new VaddioAvBridgePresentationControl(this, 3));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;
			OnConnectedStateChanged = null;

			Unsubscribe(m_SerialBuffer);
			Unsubscribe(m_SerialQueue);

			m_ConnectionStateManager.OnConnectedStateChanged -= PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.Dispose();

			base.DisposeFinal(disposing);

			m_Components.Dispose();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Connect to the AV Bridge.
		/// </summary>
		[PublicAPI]
		public void Connect()
		{
			m_ConnectionStateManager.Connect();
		}

		/// <summary>
		/// Disconnect from the AV Bridge.
		/// </summary>
		[PublicAPI]
		public void Disconnect()
		{
			m_ConnectionStateManager.Disconnect();
		}

		/// <summary>
		/// Sets the port for communicating with the device.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			m_ConnectionStateManager.SetPort(port);
			m_SerialQueue.SetPort(port);
		}

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		private void ConfigurePort(IPort port)
		{
			// Com
			if (port is IComPort)
				(port as IComPort).ApplyDeviceConfiguration(m_ComSpecProperties);

			// Network (TCP, UDP, SSH)
			if (port is ISecureNetworkPort)
				(port as ISecureNetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
			else if (port is INetworkPort)
				(port as INetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
		}

		/// <summary>
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		public void SendCommand(string command)
		{
			SendCommand(command, null);
		}

		/// <summary>
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		public void SendCommand(string command, params object[] args)
		{
			if (args != null)
				command = string.Format(command, args);

			m_SerialQueue.Enqueue(new SerialData(command + END_OF_LINE));
		}

		public void RegisterFeedback(string commandKey, Action<VaddioAvBridgeSerialResponse> callback)
		{
			if (commandKey == null)
				throw new ArgumentNullException("commandKey");

			if (callback == null)
				throw new ArgumentNullException("callback");

			if (!m_FeedbackHandlers.ContainsKey(commandKey))
				m_FeedbackHandlers.Add(commandKey, new IcdHashSet<Action<VaddioAvBridgeSerialResponse>>());

			m_FeedbackHandlers[commandKey].Add(callback);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_ConnectionStateManager != null && m_ConnectionStateManager.IsOnline;
		}

		#endregion

		#region Port Callbacks

		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs e)
		{
			UpdateCachedOnlineStatus();
		}

		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs e)
		{
			if (!e.Data)
			{
				m_SerialBuffer.Clear();

				Logger.Log(eSeverity.Critical, "Lost connection");
				Initialized = false;
			}

			OnConnectedStateChanged.Raise(this, new BoolEventArgs(e.Data));
		}

		#endregion

		#region Serial Buffer Callbacks

		private void Subscribe(VaddioAvBridgeSerialBuffer serialBuffer)
		{
			serialBuffer.OnUsernamePrompt += SerialBufferOnUsernamePrompt;
			serialBuffer.OnPasswordPrompt += SerialBufferOnPasswordPrompt;
			serialBuffer.OnWelcomePrompt += SerialBufferOnWelcomePrompt;
		}

		private void SerialBufferOnWelcomePrompt(object sender, EventArgs e)
		{
			// Re-initialize on welcome prompt.
			Initialized = false;
			Initialized = true;
		}

		private void Unsubscribe(VaddioAvBridgeSerialBuffer serialBuffer)
		{
			serialBuffer.OnUsernamePrompt -= SerialBufferOnUsernamePrompt;
			serialBuffer.OnPasswordPrompt -= SerialBufferOnPasswordPrompt;
			serialBuffer.OnWelcomePrompt -= SerialBufferOnWelcomePrompt;
		}

		private void SerialBufferOnUsernamePrompt(object sender, EventArgs args)
		{
			m_ConnectionStateManager.Send(Username + END_OF_LINE);
		}

		private void SerialBufferOnPasswordPrompt(object sender, EventArgs args)
		{
			m_ConnectionStateManager.Send(Password + END_OF_LINE);
		}

		#endregion

		#region Serial Queue

		private void Subscribe(SerialQueue serialQueue)
		{
			serialQueue.OnSerialResponse += SerialQueueOnSerialResponse;
			serialQueue.OnTimeout += SerialQueueOnTimeout;
		}

		private void Unsubscribe(SerialQueue serialQueue)
		{
			serialQueue.OnSerialResponse -= SerialQueueOnSerialResponse;
			serialQueue.OnTimeout -= SerialQueueOnTimeout;
		}

		private void SerialQueueOnTimeout(object sender, SerialDataEventArgs e)
		{
			Logger.Log(eSeverity.Error, "Command timed out - {0}", e.Data.Serialize());
		}

		private void SerialQueueOnSerialResponse(object sender, SerialResponseEventArgs args)
		{
			string data = args.Response;
			if (string.IsNullOrEmpty(data))
				return;

			if (m_ConnectionStateManager.IsConnected)
				Initialized = true;

			var response = new VaddioAvBridgeSerialResponse(data);
			HandleResponse(response);
		}

		private void HandleResponse(VaddioAvBridgeSerialResponse response)
		{
			// No key from either an error or junk data.
			if (response.Command == null)
				return;

			if (response.StatusCode != SUCCESS_STATUS_CODE)
			{
				Logger.Log(eSeverity.Error, "AV Bridge device encountered an error - {0}", response.StatusCode);
				return;
			}

			IcdHashSet<Action<VaddioAvBridgeSerialResponse>> handlers;
			if (!m_FeedbackHandlers.TryGetValue(response.Command, out handlers))
				return;

			foreach (var handler in handlers)
				try
				{
					handler(response);
				}
				catch (Exception e)
				{
					Logger.Log(eSeverity.Error, "Failed to handle feedback {0} - {1}", response.Command, e.Message);
				}
		}

		#endregion

		#region Settings

		protected override void CopySettingsFinal(VaddioAvBridgeDeviceSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_ConnectionStateManager.PortNumber;
			settings.Username = Username;
			settings.Password = Password;

			settings.Copy(m_ComSpecProperties);
			settings.Copy(m_NetworkProperties);
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Username = null;
			Password = null;

			m_ComSpecProperties.ClearComSpecProperties();
			m_NetworkProperties.ClearNetworkProperties();

			SetPort(null);
		}

		protected override void ApplySettingsFinal(VaddioAvBridgeDeviceSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_ComSpecProperties.Copy(settings);
			m_NetworkProperties.Copy(settings);

			Username = settings.Username;
			Password = settings.Password;

			ISerialPort port = null;

			if (settings.Port != null)
			{
				try
				{
					port = factory.GetPortById((int)settings.Port) as ISerialPort;
				}
				catch (KeyNotFoundException)
				{
					Logger.Log(eSeverity.Error, "No serial port with id {0}", settings.Port);
				}
			}

			SetPort(port);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			if (m_ConnectionStateManager != null && m_ConnectionStateManager.Port != null)
				yield return m_ConnectionStateManager.Port;

			yield return ConsoleNodeGroup.IndexNodeMap("Components", m_Components.GetComponents()
																				 .OrderBy(c => c.GetType().Name)
																				 .Cast<IConsoleNodeBase>());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Connected", m_ConnectionStateManager.IsConnected);
			addRow("Initialized", Initialized);
		}

		#endregion
	}
}