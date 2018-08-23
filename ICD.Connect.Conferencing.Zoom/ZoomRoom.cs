using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.Zoom.Components;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Controls;
using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Connect.Devices.EventArguments;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Heartbeat;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings.Core;
using Newtonsoft.Json;

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

		private class ResponseCallbackPair
		{
			public ResponseCallback WrappedCallback { get; set; }
			public object ActualCallback { get; set; }
		}

		private const string END_OF_LINE = "\r\n";

		/// <summary>
		/// System Configuration Commands
		/// </summary>
		private readonly string[] m_ConfigurationCommands =
		{
			"echo off",
			"format json",
		};

		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		private readonly ConnectionStateManager m_ConnectionStateManager;
		private readonly JsonSerialBuffer m_SerialBuffer;
		private readonly Dictionary<Type, List<ResponseCallbackPair>> m_ResponseCallbacks;
		private readonly SafeCriticalSection m_ResponseCallbacksSection;

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
			m_ResponseCallbacks = new Dictionary<Type, List<ResponseCallbackPair>>();
			m_ResponseCallbacksSection = new SafeCriticalSection();

			m_ConnectionStateManager = new ConnectionStateManager(this);
			Subscribe(m_ConnectionStateManager);

			m_SerialBuffer = new JsonSerialBuffer();
			Subscribe(m_SerialBuffer);

			Components = new ZoomRoomComponentFactory(this);

			Controls.Add(new ZoomRoomRoutingControl(this, Controls.Count));
			Controls.Add(new ZoomRoomDirectoryControl(this, Controls.Count));
			Controls.Add(new ZoomRoomPresentationControl(this, Controls.Count));
			Controls.Add(new ZoomRoomDialingControl(this, Controls.Count));
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
			var settings = new JsonSerializerSettings();
			settings.Converters.Add(new ZoomRoomResponseConverter());

			AbstractZoomRoomResponse response = null;
			try
			{
				response = JsonConvert.DeserializeObject<AbstractZoomRoomResponse>(json, settings);
			}
			catch (JsonReaderException)
			{
				// zoom gives us bad json (unescaped characters) in some error messages
			}
			
			if (response != null)
				CallResponseCallbacks(response);
		}

		#endregion

		#region Settings

		protected override void ApplySettingsFinal(ZoomRoomSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			if (settings.Port != null)
			{
				var port = factory.GetOriginatorById<ISerialPort>(settings.Port.Value);
				m_ConnectionStateManager.SetPort(port);
			}
		}

		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			m_ConnectionStateManager.SetPort(null);
		}

		protected override void CopySettingsFinal(ZoomRoomSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_ConnectionStateManager.PortNumber;
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "The Zoom Room conferencing device"; } }

		#endregion
	}
}