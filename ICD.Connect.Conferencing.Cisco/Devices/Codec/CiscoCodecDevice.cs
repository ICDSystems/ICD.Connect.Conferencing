using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Calender;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Utils;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Devices.Controls;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Network.Ports;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec
{
	/// <summary>
	/// Cisco VTC Codec Control
	/// </summary>
	public sealed class CiscoCodecDevice : AbstractVideoConferenceDevice<CiscoCodecSettings>
	{
		/// <summary>
		/// Callback for parser events.
		/// </summary>
		/// <param name="codec"></param>
		/// <param name="resultId"></param>
		/// <param name="xml"></param>
		public delegate void ParserCallback(CiscoCodecDevice codec, string resultId, string xml);

		/// <summary>
		/// End of line string.
		/// </summary>
		private const string END_OF_LINE = "\x0D\x0A";

		/// <summary>
		/// Prefix for configuration commands.
		/// </summary>
		public const string XCONFIGURATION_ELEMENT = "Configuration";

		/// <summary>
		/// Prefix for events feedback.
		/// </summary>
		public const string XEVENT_ELEMENT = "Event";

		/// <summary>
		/// Prefix for status feedback.
		/// </summary>
		public const string XSTATUS_ELEMENT = "Status";

		private const long FEEDBACK_MILLISECONDS = 10 * 60 * 1000;

		private const string FEEDBACK_TIMER_CALLBACK_ID = "FeedbackTimerCallback";

		/// <summary>
		/// Raised when the class initializes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		/// <summary>
		/// Raised when the codec becomes connected or disconnected.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		/// <summary>
		/// System Configuration Commands
		/// </summary>
		private readonly string[] m_ConfigurationCommands =
		{
			"echo off",
			"xPreferences outputmode xml",

			// For catching errors
			"xFeedback Register Result"
		};

		private readonly CiscoCallbackNode m_RootCallbackNode;
		private readonly ISerialBuffer m_SerialBuffer;
		private readonly SafeTimer m_FeedbackTimer;

		private readonly ConnectionStateManager m_ConnectionStateManager;

		private readonly CiscoComponentFactory m_Components;

		private readonly SecureNetworkProperties m_NetworkProperties;
		private readonly ComSpecProperties m_ComSpecProperties;

		private bool m_Initialized;
		private readonly CiscoCodecTelemetryComponent m_TelemetryComponent;

		private readonly SafeCriticalSection m_CallbacksSection;
		private readonly Dictionary<string, ParserCallback> m_ResultIdCallbacks;

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
		/// The id used to register the device with the physical codec.
		/// </summary>
		public string PeripheralsId { get; private set; }

		/// <summary>
		/// Gets the phonebook to use with the directory.
		/// </summary>
		public ePhonebookType PhonebookType { get; private set; }

		/// <summary>
		/// Determines which camera to use with PresenterTrack features.
		/// </summary>
		public int? PresenterTrackCameraId { get; set; }

		/// <summary>
		/// Provides the components attached to this codec.
		/// </summary>
		public CiscoComponentFactory Components { get { return m_Components; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "The Cisco codec device"; } }

		/// <summary>
		/// If true, the system will go into halfwake mode instead of sleep mode when standby/poweroff is run
		/// </summary>
		/// <remarks>This is a workaround for a compatibility issue with Room Kit Plus and DMPS3 rooms</remarks>
		public bool StandbyToHalfwake { get; private set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public CiscoCodecDevice()
		{
			m_NetworkProperties = new SecureNetworkProperties();
			m_ComSpecProperties = new ComSpecProperties();

			m_CallbacksSection = new SafeCriticalSection();
			m_RootCallbackNode = new CiscoCallbackNode();
			m_ResultIdCallbacks = new Dictionary<string, ParserCallback>();

			m_ConnectionStateManager = new ConnectionStateManager(this) { ConfigurePort = ConfigurePort };
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived += PortOnSerialDataReceived;

			m_Components = new CiscoComponentFactory(this);
			m_FeedbackTimer = SafeTimer.Stopped(FeedbackTimerCallback);

			m_SerialBuffer = new XmlSerialBuffer();
			Subscribe(m_SerialBuffer);

			m_TelemetryComponent = new CiscoCodecTelemetryComponent(this);
		}

		#endregion

		#region Methods

        /// <summary>
        /// Connect to the codec.
        /// </summary>
        [PublicAPI]
        public void Connect()
        {
            m_ConnectionStateManager.Connect();
        }
        
        /// <summary>
        /// Disconnect from the codec.
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
	        m_ConnectionStateManager.SetPort(port, false);
        }
        
        /// <summary>
        /// Release resources.
        /// </summary>
        protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;
			OnConnectedStateChanged = null;

			m_CallbacksSection.Execute(() => m_ResultIdCallbacks.Clear());

			m_FeedbackTimer.Dispose();

			m_ConnectionStateManager.OnConnectedStateChanged -= PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived -= PortOnSerialDataReceived;
			m_ConnectionStateManager.Dispose();

			Unsubscribe(m_SerialBuffer);

			base.DisposeFinal(disposing);

			m_Components.Dispose();
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
			SendCommand(command, (ParserCallback)null);
		}

		/// <summary>
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		public void SendCommand(string command, params object[] args)
		{
			SendCommand(command, (ParserCallback)null, args);
		}

		/// <summary>
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="callback"></param>
		public string SendCommand(string command, [CanBeNull] ParserCallback callback)
		{
			return SendCommand(command, callback, null);
		}

		/// <summary>
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="callback"></param>
		/// <param name="args"></param>
		public string SendCommand(string command, [CanBeNull] ParserCallback callback, params object[] args)
		{
			if (args != null)
				command = string.Format(command, args);

			// If a callback is specified set up a response id and handler
			string resultId = null;
			if (callback != null)
			{
				resultId = Guid.NewGuid().ToString();
				command = string.Format("{0} | resultId=\"{1}\"", command, resultId);
				m_ResultIdCallbacks.Add(resultId, callback);
			}

			m_ConnectionStateManager.Send(command + END_OF_LINE);

#if !SIMPLSHARP
			// Too fast!
			ThreadingUtils.Sleep(10);
#endif
			return resultId;
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
		/// Path corresponds to position in xml document, e.g. "Status", "Command", etc.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="path"></param>
		public void RegisterParserCallback(ParserCallback callback, params string[] path)
		{
			string key = XmlPathToKey(path);

			m_RootCallbackNode.RegisterCallback(callback, path);

			if (Initialized)
				RegisterFeedback(key);
		}

		/// <summary>
		/// Unregisters callbacks registered via RegisterParserCallback.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="path"></param>
		[PublicAPI]
		public void UnregisterParserCallback(ParserCallback callback, params string[] path)
		{
			string key = XmlPathToKey(path);

			m_RootCallbackNode.UnregisterCallback(callback, path);

			ParserCallback[] callbacks = m_RootCallbackNode.GetCallbacks(path).ToArray();
			if (callbacks.Length > 0)
				return;

			if (m_ConnectionStateManager.IsConnected)
				DeregisterFeedback(key);
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

		/// <summary>
		/// Initialize the CODEC.
		/// </summary>
		private void Initialize()
		{
			SendCommands(m_ConfigurationCommands);

			m_FeedbackTimer.Reset(FEEDBACK_MILLISECONDS, FEEDBACK_MILLISECONDS);

			// Register feedback immediately
			IEnumerable<string> keys = m_RootCallbackNode.GetPathsRecursive().Select(s => XmlPathToKey(s));
			foreach (string key in keys)
				RegisterFeedback(key);

			Initialized = true;
		}

		private void RegisterFeedback(string key)
		{
			SendCommand("xFeedback Register {0}", key);

			// If we're registering for xStatus feedback we can go ahead and
			// check the status to get some initial values.
			string[] path = KeyToXmlPath(key);
			if (string.Equals(path[0], XSTATUS_ELEMENT, StringComparison.CurrentCultureIgnoreCase))
				SendCommand(PathToCommand(path));
		}

		/// <summary>
		/// Converts a feedback path (e.g. ["status", "cameras"]) to a command (e.g. "xstatus cameras")
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static string PathToCommand(IEnumerable<string> path)
		{
			string output = string.Join(" ", path.Select(s => StringUtils.UppercaseFirst(s)).ToArray());

			if (!output.ToLower().StartsWith("x"))
				output = "x" + output;

			return output;
		}

		private void DeregisterFeedback(string key)
		{
			SendCommand("xFeedback Deregister {0}", key);
		}

		/// <summary>
		/// Generates a key for the callback dict.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		private static string XmlPathToKey(IEnumerable<string> path)
		{
			return "/" + string.Join("/", path.Select(s => s.ToLower()).ToArray());
		}

		/// <summary>
		/// Performs the inverse of XmlPathToKey.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		private static string[] KeyToXmlPath(string key)
		{
			key = key.Substring(1);
			return key.Split('/');
		}

		/// <summary>
		/// Parse and log the codec error message.
		/// </summary>
		/// <param name="xml"></param>
		private void ParseError(string xml)
		{
			string message;

			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.ReadToNextElement();

				message = reader.Name;

				foreach (IcdXmlReader child in reader.GetChildElements())
				{
					string value = child.ReadElementContentAsString();
					if (!string.IsNullOrEmpty(value))
						message = string.Format("{0} : {1}", message, value);

					child.Dispose();
				}
			}

			Logger.Log(eSeverity.Error, message);
		}

		/// <summary>
		/// Periodically re-register feedback to make sure nothing is unsubscribed.
		/// </summary>
		private void FeedbackTimerCallback()
		{
			if (m_ConnectionStateManager.IsConnected)
				SendCommand("xFeedback List | resultId=\"{0}\"", FEEDBACK_TIMER_CALLBACK_ID);
		}

		/// <summary>
		/// Parse the feedback registration and ensure nothing has been unsubscribed.
		/// </summary>
		/// <param name="xml"></param>
		private void ParseFeedbackRegistration(string xml)
		{
			string inner = XmlUtils.ReadElementContent(xml);

			IcdHashSet<string> actual = new IcdHashSet<string>(inner.Split());
			IcdHashSet<string> expected = m_RootCallbackNode.GetPathsRecursive().Select(s => XmlPathToKey(s)).ToIcdHashSet();
			IcdHashSet<string> missing = expected.Subtract(actual);

			foreach (string item in missing)
			{
				Logger.Log(eSeverity.Warning, "Lost feedback on {0}, re-registering.", item);
				RegisterFeedback(item);
			}
		}

		#endregion

		#region Port Callbacks

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
			m_CallbacksSection.Execute(() => m_ResultIdCallbacks.Clear());

			if (args.Data)
				Initialize();
			else
			{
				Logger.Log(eSeverity.Critical, "Lost connection");
				Initialized = false;
			}

			OnConnectedStateChanged.Raise(this, new BoolEventArgs(args.Data));
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
			string xml = args.Data;

			// Pull the resultId from the xml
			string resultId;
			string innerXml;

			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				if (!reader.ReadToNextElement())
					return;

				resultId = reader.GetAttribute("resultId");
				innerXml = reader.ReadInnerXml();
			}

			// Parse feedback registration
			if (resultId == FEEDBACK_TIMER_CALLBACK_ID)
			{
				ParseFeedbackRegistration(xml);
				return;
			}

			m_CallbacksSection.Enter();
			try
			{
				ParserCallback callback;
				if (m_ResultIdCallbacks.TryGetValue(resultId, out callback))
				{
					callback(this, resultId, innerXml);
					m_ResultIdCallbacks.Remove(resultId);
					return;
				}
			}
			finally
			{
				m_CallbacksSection.Leave();
			}

			// Recurse through the elements
			try
			{
				if(!StringUtils.IsNullOrWhitespace(innerXml))
					XmlUtils.Recurse(innerXml, eventArgs => XmlCallback(resultId, eventArgs));
			}
			catch (IcdXmlException e)
			{
				Logger.Log(eSeverity.Error, "Failed to parse XML - {0} - {1}", e.Message, innerXml);
			}
		}

		/// <summary>
		/// Called for each element in the xml.
		/// </summary>
		/// <param name="resultId"></param>
		/// <param name="args"></param>
		/// <returns>True to keep walking the xml document.</returns>
		private bool XmlCallback(string resultId, XmlRecursionEventArgs args)
		{
			string xml = args.Outer;
			string status = XmlUtils.GetAttribute(xml, "status");

			switch (status)
			{
				case "Error":
					ParseError(xml);
					return false;

				default:
					string key = XmlPathToKey(args.Path);
					return CallParserCallbacks(xml, resultId, args.Path);
			}
		}

		/// <summary>
		/// Calls the parser callbacks for the given path.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="resultId"></param>
		/// <param name="path"></param>
		/// <returns>True to keep walking the xml document.</returns>
		private bool CallParserCallbacks(string xml, string resultId, string[] path)
		{
			CiscoCallbackNode leaf = m_RootCallbackNode.GetChild(path);
			if (leaf == null)
				return false;

			IEnumerable<ParserCallback> callbacks = leaf.GetCallbacks();
			foreach (ParserCallback callback in callbacks)
				callback(this, resultId, xml);

			return leaf.GetChildren().Any();
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(CiscoCodecSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_ConnectionStateManager.PortNumber;
			settings.PeripheralsId = PeripheralsId;
			settings.PhonebookType = PhonebookType;

			settings.Copy(m_ComSpecProperties);
			settings.Copy(m_NetworkProperties);

			settings.PresenterTrackCameraId = PresenterTrackCameraId;
			settings.StandbyToHalfwake = StandbyToHalfwake;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			PeripheralsId = null;
			PhonebookType = ePhonebookType.Corporate;
			PresenterTrackCameraId = null;
			StandbyToHalfwake = false;

			m_ComSpecProperties.ClearComSpecProperties();
			m_NetworkProperties.ClearNetworkProperties();

			SetPort(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(CiscoCodecSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			m_ComSpecProperties.Copy(settings);
			m_NetworkProperties.Copy(settings);

			PeripheralsId = settings.PeripheralsId;
			PhonebookType = settings.PhonebookType;
			PresenterTrackCameraId = settings.PresenterTrackCameraId;
			StandbyToHalfwake = settings.StandbyToHalfwake;

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

		/// <summary>
		/// Override to add controls to the device.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		/// <param name="addControl"></param>
		protected override void AddControls(CiscoCodecSettings settings, IDeviceFactory factory,
		                                    Action<IDeviceControl> addControl)
		{
			base.AddControls(settings, factory, addControl);

			addControl(new CiscoCodecRoutingControl(this, 0));
			addControl(new CiscoCodecConferenceControl(this, 1));
			addControl(new CiscoCodecDirectoryControl(this, 2));
			addControl(new CiscoCodecLayoutControl(this, 3));
			addControl(new CiscoCodecPresentationControl(this, 4));
			addControl(new CiscoCodecPowerControl(this, 5));
			addControl(new CiscoCodecCalendarControl(this, 6));
			addControl(new CiscoCodecOccupancySensorControl(this, 7));
			addControl(new CiscoCodecVolumeControl(this, 8));
			addControl(new CiscoCodecDirectSharingControl(this, 9));
		}

		/// <summary>
		/// Override to add actions on StartSettings
		/// This should be used to start communications with devices and perform initial actions
		/// </summary>
		protected override void StartSettingsFinal()
		{
			base.StartSettingsFinal();

			m_ConnectionStateManager.Start();
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
			addRow("Peripherals ID", PeripheralsId);
			addRow("Phonebook Type", PhonebookType);
		}

		#endregion
	}
}
