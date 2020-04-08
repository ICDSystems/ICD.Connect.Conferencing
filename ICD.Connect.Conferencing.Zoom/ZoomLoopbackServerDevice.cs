using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Nodes;
using ICD.Connect.Devices;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.EventArguments;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Ports.Tcp;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Zoom
{
	public sealed class ZoomLoopbackServerDevice : AbstractDevice<ZoomLoopbackServerSettings>
	{
		/// <summary>
		/// End of line character(s) for ZR-CSAPI commands.
		/// Must be \r, since the API doesn't accept the "format json" command otherwise.
		/// </summary>
		private const string END_OF_LINE = "\r";

		/// <summary>
		/// Messages sent from the loopback device to the clients are delimited with this character.
		/// </summary>
		public const char CLIENT_DELIMITER = '\xFF';

		private readonly ConnectionStateManager m_ConnectionStateManager;
		private readonly JsonSerialBuffer m_SerialBuffer;

		private readonly IcdTcpServer m_TcpServer;
		private readonly TcpServerBufferManager m_ClientBuffers;

		private readonly SecureNetworkProperties m_NetworkProperties;

		#region Properties

		/// <summary>
		/// Device Initialized Status.
		/// </summary>
		public bool Initialized { get; private set; }

		/// <summary>
		/// Returns true when the codec is connected.
		/// </summary>
		public bool IsConnected { get { return m_ConnectionStateManager.IsConnected; } }

		public string ListenAddress
		{
			get { return m_TcpServer.AddressToAcceptConnectionFrom; }
			set { m_TcpServer.AddressToAcceptConnectionFrom = value; }
		}

		public ushort ListenPort { get { return m_TcpServer.Port; } set { m_TcpServer.Port = value; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ZoomLoopbackServerDevice()
		{
			m_NetworkProperties = new SecureNetworkProperties();

			m_TcpServer = new IcdTcpServer(2245, IcdTcpServer.MAX_NUMBER_OF_CLIENTS_SUPPORTED);
			Subscribe(m_TcpServer);

			m_ClientBuffers = new TcpServerBufferManager(() => new DelimiterSerialBuffer('\r'));
			m_ClientBuffers.SetServer(m_TcpServer);
			Subscribe(m_ClientBuffers);

			m_ConnectionStateManager = new ConnectionStateManager(this) {ConfigurePort = ConfigurePort};
			Subscribe(m_ConnectionStateManager);

			m_SerialBuffer = new JsonSerialBuffer();
			Subscribe(m_SerialBuffer);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_TcpServer);
			Unsubscribe(m_ClientBuffers);
			Unsubscribe(m_SerialBuffer);
			Unsubscribe(m_ConnectionStateManager);

			m_TcpServer.Dispose();
		}

		#region Methods

		/// <summary>
		/// Start connecting to the zoom device.
		/// </summary>
		public void Start()
		{
			m_ConnectionStateManager.Start();
			m_TcpServer.Start();
		}

		/// <summary>
		/// Disconnect and stop connecting to the zoom device.
		/// </summary>
		public void Stop()
		{
			m_TcpServer.Stop();
			m_ConnectionStateManager.Stop();
		}

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_ConnectionStateManager.IsOnline;
		}

		/// <summary>
		/// Sets the port for communicating with the device.
		/// </summary>
		/// <param name="port"></param>
		/// <param name="monitor"></param>
		[PublicAPI]
		public void SetPort(ISerialPort port, bool monitor)
		{
			m_ConnectionStateManager.SetPort(port, monitor);
		}

		/// <summary>
		/// Configures the given port for communication with the device.
		/// </summary>
		/// <param name="port"></param>
		private void ConfigurePort(ISerialPort port)
		{
			// SSH
			if (port is ISecureNetworkPort)
				(port as ISecureNetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
			// TCP
			else if (port is INetworkPort)
				(port as INetworkPort).ApplyDeviceConfiguration(m_NetworkProperties);
		}

		/// <summary>
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		public void SendCommand(string command)
		{
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
		/// Initialize the Zoom Room API.
		/// </summary>
		private void Initialize()
		{
			m_ConnectionStateManager.Send("echo off");
			m_ConnectionStateManager.Send("\r");
			m_ConnectionStateManager.Send("format json");
			m_ConnectionStateManager.Send("\r");
		}

		#endregion

		#region Port Callbacks

		/// <summary>
		/// Subscribes to the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Subscribe(ConnectionStateManager port)
		{
			port.OnSerialDataReceived += PortOnSerialDataReceived;
			port.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			port.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Unsubscribe(ConnectionStateManager port)
		{
			port.OnSerialDataReceived -= PortOnSerialDataReceived;
			port.OnConnectedStateChanged -= PortOnConnectionStatusChanged;
			port.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Called when serial data is recieved from the port.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnSerialDataReceived(object sender, StringEventArgs args)
		{
			if (args.Data.StartsWith("{"))
			{
				// Hack - JSON buffering is bad and SSH messages are always(?) complete anyway
				SerialBufferCompletedSerial(this, args);
				Initialized = true;
			}
			else if (args.Data.Contains("Login") || args.Data.StartsWith("*"))
				Initialize();
			else if (!Initialized && args.Data == "\n")
				SendCommand("zStatus Call Status");
		}

		/// <summary>
		/// Called when the port connection status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs args)
		{
			m_SerialBuffer.Clear();

			if (!args.Data)
			{
				Logger.Log(eSeverity.Critical, "Lost connection");
				Initialized = false;
			}
		}

		/// <summary>
		/// Called when the port online status changes.1
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs args)
		{
			UpdateCachedOnlineStatus();
		}

		#endregion

		#region Buffer Callbacks

		/// <summary>
		/// Subscribes to the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Subscribe(ISerialBuffer buffer)
		{
			buffer.OnCompletedSerial += SerialBufferCompletedSerial;
		}

		/// <summary>
		/// Unsubscribe from the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Unsubscribe(ISerialBuffer buffer)
		{
			buffer.OnCompletedSerial -= SerialBufferCompletedSerial;
		}

		/// <summary>
		/// Called when the buffer completes a string.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void SerialBufferCompletedSerial(object sender, StringEventArgs args)
		{
			/*
			AttributeKey key;
			AbstractZoomRoomResponse response;

			try
			{
				response = AbstractZoomRoomResponse.DeserializeResponse(args.Data, out key);
				if (response == null)
					return;
			}
			catch (Exception ex)
			{
				// Zoom gives us bad json (unescaped characters) in some error messages
				Log(eSeverity.Error, ex, "Failed to deserialize JSON");
				return;
			}

			string minified = JsonConvert.SerializeObject(response);
			if (minified.Length < 2)
				return;

			// Hack to re-append the key info
			minified = minified.Insert(minified.Length - 1, ", " + key);
			*/

			SendToClients(args.Data);

			Initialized = true;
		}

		private void SendToClients(string json)
		{
			m_TcpServer.Send(json + CLIENT_DELIMITER);
		}

		#endregion

		#region TCP Server Callbacks

		/// <summary>
		/// Subscribe to the TCP server events.
		/// </summary>
		/// <param name="tcpServer"></param>
		private void Subscribe(IcdTcpServer tcpServer)
		{
			tcpServer.OnSocketStateChange += TcpServerOnSocketStateChange;
		}

		/// <summary>
		/// Unsubscribe from the TCP server events.
		/// </summary>
		/// <param name="tcpServer"></param>
		private void Unsubscribe(IcdTcpServer tcpServer)
		{
			tcpServer.OnSocketStateChange -= TcpServerOnSocketStateChange;
		}

		/// <summary>
		/// Called when a client connects/disconnects
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void TcpServerOnSocketStateChange(object sender, SocketStateEventArgs eventArgs)
		{
			// Hack - tell the client to initialize
			if (eventArgs.SocketState == SocketStateEventArgs.eSocketStatus.SocketStatusConnected)
				m_TcpServer.Send(eventArgs.ClientId, "\n" + CLIENT_DELIMITER);
		}

		#endregion

		#region Client Buffer Callbacks

		/// <summary>
		/// Subscribe to the client buffers events.
		/// </summary>
		/// <param name="clientBuffers"></param>
		private void Subscribe(TcpServerBufferManager clientBuffers)
		{
			clientBuffers.OnClientCompletedSerial += ClientBuffersOnClientCompletedSerial;
		}

		/// <summary>
		/// Subscribe to the client buffers events.
		/// </summary>
		/// <param name="clientBuffers"></param>
		private void Unsubscribe(TcpServerBufferManager clientBuffers)
		{
			clientBuffers.OnClientCompletedSerial -= ClientBuffersOnClientCompletedSerial;
		}

		/// <summary>
		/// Called when we receive a message from a client.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="clientId"></param>
		/// <param name="data"></param>
		private void ClientBuffersOnClientCompletedSerial(TcpServerBufferManager sender, uint clientId, string data)
		{
			// Pass the command on to the Zoom device.
			SendCommand(data);
		}

		#endregion

		#region Settings

		protected override void ApplySettingsFinal(ZoomLoopbackServerSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			ListenAddress = settings.ListenAddress;
			ListenPort = settings.ListenPort;

			m_NetworkProperties.Copy(settings);

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

			SetPort(port, true);

			m_TcpServer.Start();
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_NetworkProperties.ClearNetworkProperties();

			m_TcpServer.Stop();
			ListenAddress = "0.0.0.0";
			ListenPort = 2245;

			SetPort(null, false);
		}

		protected override void CopySettingsFinal(ZoomLoopbackServerSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_ConnectionStateManager.PortNumber;

			settings.ListenAddress = ListenAddress;
			settings.ListenPort = ListenPort;

			settings.Copy(m_NetworkProperties);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "The Zoom Loopback Server device"; } }

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Initialized", Initialized);
			addRow("IsConnected", IsConnected);
			addRow("Loopback Port", m_TcpServer.Port);
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
	}
}
