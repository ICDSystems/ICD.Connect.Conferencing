using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol.Heartbeat;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom
{
	public sealed class ZoomRoom : AbstractVideoConferenceDevice<ZoomRoomSettings>, IConnectable
	{
		/// <summary>
		/// Wrapper callback for responses that casts to the appropriate type.
		/// </summary>
		private delegate void ResponseCallback(ZoomRoom zoomRoom, AbstractZoomRoomResponse response);
		/// <summary>
		/// Callback for responses.
		/// </summary>
		public delegate void ResponseCallback<T>(ZoomRoom zoomRoom, T response) where T : AbstractZoomRoomResponse;

		private class ResponseCallbackPair
		{
			public ResponseCallback WrappedCallback { get; set; }
			public object ActualCallback { get; set; }
		}

		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		private bool m_Initialized;
		private bool m_IsConnected;
		private ISerialPort m_Port;
		private JsonSerialBuffer m_SerialBuffer;
		private const string END_OF_LINE = "\n";

		/// <summary>
		/// System Configuration Commands
		/// </summary>
		private readonly string[] m_ConfigurationCommands =
		{
			"echo off",
			"format json",
		};

		private readonly Dictionary<Type, List<ResponseCallbackPair>> m_ResponseCallbacks;
		private readonly SafeCriticalSection m_ResponseCallbacksSection;

		#region Properties

		public Heartbeat Heartbeat { get; private set; }

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
		public bool IsConnected
		{
			get { return m_IsConnected; }
			private set
			{
				if (value == m_IsConnected)
					return;

				m_IsConnected = value;

				OnConnectedStateChanged.Raise(this, new BoolEventArgs(m_IsConnected));
			}
		}

		public CallComponent CurrentCall { get; private set; }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "The Zoom Room conferencing device"; } }

		/// <summary>
		/// Causes this ZoomRoom to auto-accept any incoming calls.
		/// </summary>
		public bool AutoAnswer { get; set; }

		/// <summary>
		/// Causes this ZoomRoom to auto-reject any incoming calls. Overrides AutoAnswer
		/// </summary>
		public bool DoNotDisturb { get; set; }

		#endregion

		#region Constructors

		public ZoomRoom()
		{
			Heartbeat = new Heartbeat(this);

			m_ResponseCallbacks = new Dictionary<Type, List<ResponseCallbackPair>>();
			m_ResponseCallbacksSection = new SafeCriticalSection();
		}
		#endregion

		#region Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Port != null && m_Port.IsOnline;
		}

		/// <summary>
		/// Sets the port for communicating with the device.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public void SetPort(ISerialPort port)
		{
			if (port == m_Port)
				return;

			if (m_Port != null)
				Disconnect();

			Unsubscribe(m_Port);
			m_Port = port;
			Subscribe(m_Port);

			if (m_Port != null)
				Heartbeat.StartMonitoring();

			UpdateCachedOnlineStatus();
		}

		public void Connect()
		{
			if (m_Port == null)
			{
				Log(eSeverity.Critical, "Unable to connect, port is null");
				return;
			}

			m_Port.Connect();
			IsConnected = m_Port.IsConnected;

			if (IsConnected)
				Initialize();
		}

		public void Disconnect()
		{
			if (m_Port == null)
			{
				Log(eSeverity.Critical, "Unable to disconnect, port is null");
				return;
			}

			m_Port.Disconnect();
			IsConnected = m_Port.IsConnected;
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

			if (m_Port == null)
			{
				Log(eSeverity.Error, "Unable to communicate with Zoom Room - port is null");
				return;
			}

			if (!IsConnected)
			{
				Log(eSeverity.Warning, "Zoom Room is disconnected, attempting reconnect");
				Connect();
			}

			if (!IsConnected)
			{
				Log(eSeverity.Critical, "Unable to communicate with Zoom Room");
				return;
			}

			m_Port.Send(command + END_OF_LINE);
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
		/// <param name="path"></param>
		public void RegisterResponseCallback<T>(ResponseCallback<T> callback) where T : AbstractZoomRoomResponse
		{
			var wrappedCallback = WrapCallback(callback);

			m_ResponseCallbacksSection.Execute(() =>
			{
				if (!m_ResponseCallbacks.ContainsKey(typeof (T)))
					m_ResponseCallbacks.Add(typeof (T), new List<ResponseCallbackPair>());

				m_ResponseCallbacks[typeof (T)].Add(new ResponseCallbackPair
				{
					WrappedCallback = wrappedCallback,
					ActualCallback = callback
				});
			});
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
		public void UnregisterResponseCallback<T>(ResponseCallback<T> callback) where T: AbstractZoomRoomResponse
		{
			m_ResponseCallbacksSection.Enter();

			try
			{
				Type key = typeof (T);
				if (!m_ResponseCallbacks.ContainsKey(key))
					return;

				List<ResponseCallbackPair> callbackList = m_ResponseCallbacks[key];
				ResponseCallbackPair callbackToRemove = callbackList.SingleOrDefault(c => c.ActualCallback.Equals(callback));

				if (callbackToRemove == null)
					return;
				
				callbackList.Remove(callbackToRemove);
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
			SendCommands(m_ConfigurationCommands);
            SendCommand("zConfiguration Client deviceSystem: \"Krang Zoom Room Controller\"");
            SendCommand("zConfiguration Client appVersion: {0}", GetType().GetAssembly().GetName().Version);
			Initialized = true;
		}

		private void CallResponseCallbacks(AbstractZoomRoomResponse response)
		{
			Type responseType = response.GetType();
			ResponseCallback[] callbacks;

			m_ResponseCallbacksSection.Enter();
			try
			{
				if (!m_ResponseCallbacks.ContainsKey(responseType))
					return;

				callbacks = m_ResponseCallbacks[responseType].Select(c => c.WrappedCallback).ToArray();
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
		private void Subscribe(ISerialPort port)
		{
			if (port == null)
				return;

			port.OnSerialDataReceived += PortOnSerialDataReceived;
			port.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			port.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the port events.
		/// </summary>
		/// <param name="port"></param>
		private void Unsubscribe(ISerialPort port)
		{
			if (port == null)
				return;

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
			m_SerialBuffer.Enqueue(args.Data);
		}

		/// <summary>
		/// Called when the port connection status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void PortOnConnectionStatusChanged(object sender, BoolEventArgs args)
		{
			m_SerialBuffer.Clear();

			IsConnected = args.Data;

			if (IsConnected)
				Initialize();
			else
			{
				Log(eSeverity.Critical, "Lost connection");
				Initialized = false;
			}
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
			string json = args.Data;

			var settings = new JsonSerializerSettings();
			settings.Converters.Add(new ZoomRoomResponseConverter());
			var response = JsonConvert.DeserializeObject<AbstractZoomRoomResponse>(json, settings);

			CallResponseCallbacks(response);
		}

		#endregion
	}
}