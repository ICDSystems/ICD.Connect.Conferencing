using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.Zoom.Components;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Controls;
using ICD.Connect.Conferencing.Zoom.Controls.Calendar;
using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Conferencing.Zoom.Components.System;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Zoom.Components.Audio;
using ICD.Connect.Conferencing.Zoom.Components.TraditionalCall;

namespace ICD.Connect.Conferencing.Zoom
{
	public sealed class ZoomRoom : AbstractVideoConferenceDevice<ZoomRoomSettings>
	{
		/// <summary>
		/// Wrapper callback for responses that casts to the appropriate type.
		/// </summary>
		private delegate void ResponseCallback(ZoomRoom zoomRoom, AbstractZoomRoomResponse response);

		/// <summary>
		/// Callback for responses.
		/// </summary>
		public delegate void ResponseCallback<T>(ZoomRoom zoomRoom, T response) where T : AbstractZoomRoomResponse;

		private sealed class ResponseCallbackPair
		{
			public ResponseCallback WrappedCallback { get; set; }
			public object ActualCallback { get; set; }
		}

		/// <summary>
		/// End of line character(s) for ZR-CSAPI commands.
		/// Must be \r, since the API doesn't accept the "format json" command otherwise.
		/// </summary>
		private const string END_OF_LINE = "\r";

		/// <summary>
		/// Raised when the connected state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		/// <summary>
		/// Raised when the initialization state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		private readonly ConnectionStateManager m_ConnectionStateManager;
		private readonly DelimiterSerialBuffer m_SerialBuffer;
		private readonly Dictionary<Type, List<ResponseCallbackPair>> m_ResponseCallbacks;
		private readonly SafeCriticalSection m_ResponseCallbacksSection;

		private readonly SecureNetworkProperties m_NetworkProperties;

		private bool m_Initialized;

		#region Properties

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
		/// Returns true when the codec is connected.
		/// </summary>
		public bool IsConnected { get { return m_ConnectionStateManager.IsConnected; } }

		/// <summary>
		/// Causes this ZoomRoom to auto-accept any incoming calls.
		/// </summary>
		public bool AutoAnswer { get; set; }

		/// <summary>
		/// Causes this ZoomRoom to auto-reject any incoming calls. Overrides AutoAnswer
		/// </summary>
		public bool DoNotDisturb { get; set; }

		public ZoomRoomComponentFactory Components { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ZoomRoom()
		{
			m_NetworkProperties = new SecureNetworkProperties();

			m_ResponseCallbacks = new Dictionary<Type, List<ResponseCallbackPair>>();
			m_ResponseCallbacksSection = new SafeCriticalSection();

			m_ConnectionStateManager = new ConnectionStateManager(this) {ConfigurePort = ConfigurePort};
			Subscribe(m_ConnectionStateManager);

			m_SerialBuffer = new DelimiterSerialBuffer(ZoomLoopbackServerDevice.CLIENT_DELIMITER);
			Subscribe(m_SerialBuffer);

			Components = new ZoomRoomComponentFactory(this);
			// Create new system component
			Components.GetComponent<SystemComponent>();
			// Create new Audio component
			Components.GetComponent<AudioComponent>();

			Controls.Add(new ZoomRoomRoutingControl(this, Controls.Count));
			Controls.Add(new ZoomRoomDirectoryControl(this, Controls.Count));
			Controls.Add(new ZoomRoomPresentationControl(this, Controls.Count));
			Controls.Add(new ZoomRoomConferenceControl(this, Controls.Count));
			Controls.Add(new ZoomRoomTraditionalCallControl(this, Controls.Count));
			Controls.Add(new ZoomRoomCalendarControl(this, Controls.Count));
			Controls.Add(new ZoomRoomLayoutControl(this, Controls.Count));
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnConnectedStateChanged = null;
			OnInitializedChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_SerialBuffer);
			Unsubscribe(m_ConnectionStateManager);
		}

		#region Methods

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
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			m_ConnectionStateManager.SetPort(port);
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
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		public void SendCommand(string command, params object[] args)
		{
			if (args != null)
				command = string.Format(command, args);

			SendCommand(command);
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

		/// <summary>
		/// Registers the given callback.
		/// </summary>
		/// <param name="callback"></param>
		public void RegisterResponseCallback<T>(ResponseCallback<T> callback)
			where T : AbstractZoomRoomResponse
		{
			ResponseCallback wrappedCallback = WrapCallback(callback);

			m_ResponseCallbacksSection.Enter();

			try
			{		                                   
				List<ResponseCallbackPair> callbacks;
				if (!m_ResponseCallbacks.TryGetValue(typeof(T), out callbacks))
				{
					callbacks = new List<ResponseCallbackPair>();
					m_ResponseCallbacks.Add(typeof(T), callbacks);
				}

				callbacks.Add(new ResponseCallbackPair
				{
					WrappedCallback = wrappedCallback,
					ActualCallback = callback
				});
			}
			finally
			{
				m_ResponseCallbacksSection.Leave();
			}
		}

		private static ResponseCallback WrapCallback<T>(ResponseCallback<T> callback) where T : AbstractZoomRoomResponse
		{
			// separate static method to prevent lambda capture context wackiness
			return (zr, resp) => callback(zr, (T)resp);
		}

		/// <summary>
		/// Unregisters callbacks registered via RegisterResponseCallback.
		/// </summary>
		/// <param name="callback"></param>
		[PublicAPI]
		public void UnregisterResponseCallback<T>(ResponseCallback<T> callback) where T : AbstractZoomRoomResponse
		{
			m_ResponseCallbacksSection.Enter();

			try
			{
				List<ResponseCallbackPair> callbacks;
				if (!m_ResponseCallbacks.TryGetValue(typeof(T), out callbacks))
					return;

				int indexToRemove = callbacks.FindIndex(c => c.ActualCallback.Equals(callback));
				if (indexToRemove < 0)
					return;

				callbacks.RemoveAt(indexToRemove);
			}
			finally
			{
				m_ResponseCallbacksSection.Leave();
			}
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
			//m_ConnectionStateManager.Send("zStatus CallStatus\r");
		}

		private void CallResponseCallbacks(AbstractZoomRoomResponse response)
		{
			Type responseType = response.GetType();
			ResponseCallback[] callbacks;

			m_ResponseCallbacksSection.Enter();

			try
			{
				List<ResponseCallbackPair> callbackPairs;
				if (!m_ResponseCallbacks.TryGetValue(responseType, out callbackPairs))
					return;

				callbacks = callbackPairs.Select(c => c.WrappedCallback).ToArray();
			}
			finally
			{
				m_ResponseCallbacksSection.Leave();
			}

			foreach (ResponseCallback callback in callbacks)
				callback(this, response);
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
			if (args.Data.Contains("Login") || args.Data.StartsWith("*"))
				Initialize();
			else if (!Initialized && args.Data.StartsWith("\n"))
				SendCommand("zStatus Call Status");
			else
			{
				// Hack - JSON buffering is bad and SSH messages are always(?) complete anyway
				if (m_ConnectionStateManager.Port is ISecureNetworkPort)
					SerialBufferCompletedSerial(this, new StringEventArgs(args.Data));
				else
					m_SerialBuffer.Enqueue(args.Data);

				Initialized = true;
			}
		}

		/// <summary>
		/// Called when the port connection status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs args)
		{
			m_SerialBuffer.Clear();

			if (args.Data)
				return;

			Log(eSeverity.Critical, "Lost connection");
			Initialized = false;
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
			AbstractZoomRoomResponse response = null;

			try
			{
				AttributeKey unused;
				response = AbstractZoomRoomResponse.DeserializeResponse(args.Data.Trim(), out unused);
			}
			catch (Exception ex)
			{
				// Zoom gives us bad json (unescaped characters) in some error messages
				Log(eSeverity.Error, ex, "Failed to deserialize JSON");
			}

			if (response == null)
				return;

			CallResponseCallbacks(response);
			Initialized = true;
		}

		#endregion

		#region Settings

		protected override void ApplySettingsFinal(ZoomRoomSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

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
					Log(eSeverity.Error, "No serial port with id {0}", settings.Port);
				}
			}

			SetPort(port);
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_NetworkProperties.ClearNetworkProperties();

			SetPort(null);
		}

		protected override void CopySettingsFinal(ZoomRoomSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_ConnectionStateManager.PortNumber;

			settings.Copy(m_NetworkProperties);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "The Zoom Room conferencing device"; } }

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Initialized", Initialized);
			addRow("IsConnected", IsConnected);
			addRow("AutoAnswer", AutoAnswer);
			addRow("DoNotDisturb", DoNotDisturb);
		}

		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			yield return Components;
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
