using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using Newtonsoft.Json;
#if SIMPLSHARP
using Crestron.SimplSharp.Reflection;
#endif
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

namespace ICD.Connect.Conferencing.Zoom
{
	public sealed class ZoomRoom : AbstractVideoConferenceDevice<ZoomRoomSettings>
	{
		private static readonly Dictionary<AttributeKey, Type> s_TypeDict;

		/// <summary>
		/// Static constructor.
		/// </summary>
		static ZoomRoom()
		{
			s_TypeDict = new Dictionary<AttributeKey, Type>();

			foreach (
#if SIMPLSHARP
				CType
#else
				Type
#endif
					type in typeof(ZoomRoom).GetAssembly().GetTypes())
			{
				foreach (ZoomRoomApiResponseAttribute attribute in type.GetCustomAttributes<ZoomRoomApiResponseAttribute>())
				{
					AttributeKey key = new AttributeKey(attribute);
					s_TypeDict.Add(key, type);
				}
			}
		}
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

		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		private readonly ConnectionStateManager m_ConnectionStateManager;
		private readonly JsonSerialBuffer m_SerialBuffer;
		private readonly Dictionary<Type, List<ResponseCallbackPair>> m_ResponseCallbacks;
		private readonly SafeCriticalSection m_ResponseCallbacksSection;

		private readonly SecureNetworkProperties m_NetworkProperties;

		private bool m_Initialized;
		private bool m_IsConnected;

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
				{
					return;
				}

				m_Initialized = value;

				OnInitializedChanged.Raise(this, new BoolEventArgs(m_Initialized));
			}
		}

		/// <summary>
		/// Returns true when the codec is connected.
		/// </summary>
		public bool IsConnected
		{
			get { return m_ConnectionStateManager.IsConnected; }
		}

		public CallComponent CurrentCall { get; private set; }

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

		public ZoomRoom()
		{
			m_NetworkProperties = new SecureNetworkProperties();

			m_ResponseCallbacks = new Dictionary<Type, List<ResponseCallbackPair>>();
			m_ResponseCallbacksSection = new SafeCriticalSection();

			m_ConnectionStateManager = new ConnectionStateManager(this) {ConfigurePort = ConfigurePort};
			Subscribe(m_ConnectionStateManager);

			m_SerialBuffer = new JsonSerialBuffer();
			Subscribe(m_SerialBuffer);

			Components = new ZoomRoomComponentFactory(this);
			// create new system component
			Components.GetComponent<SystemComponent>();

			Controls.Add(new ZoomRoomRoutingControl(this, Controls.Count));
			Controls.Add(new ZoomRoomDirectoryControl(this, Controls.Count));
			Controls.Add(new ZoomRoomPresentationControl(this, Controls.Count));
			Controls.Add(new ZoomRoomConferenceControl(this, Controls.Count));
			Controls.Add(new ZoomRoomCalendarControl(this, Controls.Count));
		}

		protected override void DisposeFinal(bool disposing)
		{
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
			{
				command = string.Format(command, args);
			}

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
			{
				throw new ArgumentNullException("commands");
			}

			foreach (string command in commands)
			{
				SendCommand(command);
			}
		}

		/// <summary>
		/// Registers the given callback.
		/// </summary>
		/// <param name="callback"></param>
		public void RegisterResponseCallback<T>(ResponseCallback<T> callback)
			where T : AbstractZoomRoomResponse
		{
			var wrappedCallback = WrapCallback(callback);

			m_ResponseCallbacksSection.Execute(() =>
			{
				if (!m_ResponseCallbacks.ContainsKey(typeof(T)))
				{
					m_ResponseCallbacks.Add(typeof(T), new List<ResponseCallbackPair>());
				}

				m_ResponseCallbacks[typeof(T)].Add(new ResponseCallbackPair
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
		public void UnregisterResponseCallback<T>(ResponseCallback<T> callback) where T : AbstractZoomRoomResponse
		{
			m_ResponseCallbacksSection.Enter();

			try
			{
				Type key = typeof(T);
				if (!m_ResponseCallbacks.ContainsKey(key))
				{
					return;
				}

				List<ResponseCallbackPair> callbackList = m_ResponseCallbacks[key];
				ResponseCallbackPair callbackToRemove = callbackList.SingleOrDefault(c => c.ActualCallback.Equals(callback));

				if (callbackToRemove == null)
				{
					return;
				}

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
				if (!m_ResponseCallbacks.ContainsKey(responseType))
				{
					return;
				}

				callbacks = m_ResponseCallbacks[responseType].Select(c => c.WrappedCallback).ToArray();
			}
			finally
			{
				m_ResponseCallbacksSection.Leave();
			}

			foreach (ResponseCallback callback in callbacks)
			{
				callback(this, response);
			}
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
				SerialBufferCompletedSerial(sender, args);
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
				Log(eSeverity.Critical, "Lost connection");
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
			string json = args.Data;

			AbstractZoomRoomResponse response = DeserializeResponse(json);
			if (response == null)
				return;

			CallResponseCallbacks(response);
			Initialized = true;
		}

		[CanBeNull]
		private AbstractZoomRoomResponse DeserializeResponse(string data)
		{
			AttributeKey key;
			if (!AttributeKey.TryParse(data, out key))
				return null;

			AbstractZoomRoomResponse response = null;

			try
			{
				// Find concrete type that matches the json values
				Type responseType = s_TypeDict.GetDefault(key);
				if (responseType != null)
					response = JsonConvert.DeserializeObject(data, responseType) as AbstractZoomRoomResponse;
			}
			catch (Exception ex)
			{
				// Zoom gives us bad json (unescaped characters) in some error messages
				Log(eSeverity.Error, ex, "Failed to deserialize JSON");
			}

			return response;
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

		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return Components;
			foreach (var node in base.GetConsoleNodes())
				yield return node;
		}

		#endregion
	}
}