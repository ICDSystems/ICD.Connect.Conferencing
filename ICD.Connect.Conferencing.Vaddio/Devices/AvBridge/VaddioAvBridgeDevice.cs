using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Controls;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge
{
	public sealed class VaddioAvBridgeDevice : AbstractVideoConferenceDevice<VaddioAvBridgeDeviceSettings>
	{
		/// <summary>
		/// End of line string.
		/// </summary>
		private const string END_OF_LINE = "\r\n";

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

		private readonly SecureNetworkProperties m_NetworkProperties;
		private readonly ComSpecProperties m_ComSpecProperties;
		private readonly ConnectionStateManager m_ConnectionStateManager;

		private readonly AvBridgeComponentFactory m_Components;

		private readonly VaddioAvBridgeSerialBuffer m_SerialBuffer;

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
		public AvBridgeComponentFactory Components { get { return m_Components; } }

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

			m_Components = new AvBridgeComponentFactory(this);

			m_SerialBuffer = new VaddioAvBridgeSerialBuffer();
			Subscribe(m_SerialBuffer);

			m_ConnectionStateManager = new ConnectionStateManager(this) { ConfigurePort = ConfigurePort };
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived += PortOnSerialDataReceived;

			Controls.Add(new VaddioAvBridgeVolumeControl(this, 0));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;
			OnConnectedStateChanged = null;

			Unsubscribe(m_SerialBuffer);

			m_ConnectionStateManager.OnConnectedStateChanged -= PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived -= PortOnSerialDataReceived;
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
			if (port != null)
			{
				port.DebugRx = eDebugMode.Ascii;
				port.DebugTx = eDebugMode.Ascii;
			}

			m_ConnectionStateManager.SetPort(port);
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

			m_ConnectionStateManager.Send(command + END_OF_LINE);
		}

		/// <summary>
		/// Sends commands.
		/// </summary>
		/// <param name="commands"></param>
		[PublicAPI]
		public void SendCommands(params string[] commands)
		{
			if (commands == null)
				throw new ArgumentNullException("commands");

			foreach (string command in commands)
				SendCommand(command);
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

		private void PortOnSerialDataReceived(object sender, StringEventArgs e)
		{
			m_SerialBuffer.Enqueue(e.Data);
		}

		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs e)
		{
			UpdateCachedOnlineStatus();
		}

		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs e)
		{
			// TODO
			// Lose connection gracefully.

			//OnConnectedStateChanged.Raise(this, new BoolEventArgs(e.Data));
		}

		#endregion

		#region Serial Buffer Callbacks

		private void Subscribe(VaddioAvBridgeSerialBuffer serialBuffer)
		{
			serialBuffer.OnUsernamePrompt += SerialBufferOnUsernamePrompt;
			serialBuffer.OnPasswordPrompt += SerialBufferOnPasswordPrompt;
			serialBuffer.OnCompletedSerial += SerialBufferOnCompletedSerial;
		}

		private void Unsubscribe(VaddioAvBridgeSerialBuffer serialBuffer)
		{
			serialBuffer.OnUsernamePrompt -= SerialBufferOnUsernamePrompt;
			serialBuffer.OnPasswordPrompt -= SerialBufferOnPasswordPrompt;
			serialBuffer.OnCompletedSerial -= SerialBufferOnCompletedSerial;
		}

		private void SerialBufferOnUsernamePrompt(object sender, EventArgs args)
		{
			m_ConnectionStateManager.Send(Username + END_OF_LINE);
		}

		private void SerialBufferOnPasswordPrompt(object sender, EventArgs args)
		{
			m_ConnectionStateManager.Send(Password + END_OF_LINE);
		}

		private void SerialBufferOnCompletedSerial(object sender, StringEventArgs stringEventArgs)
		{
			//Handle feedback
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

		protected override void ApplySettingsFinal(VaddioAvBridgeDeviceSettings settings, Settings.IDeviceFactory factory)
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

			if (m_ConnectionStateManager != null)
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