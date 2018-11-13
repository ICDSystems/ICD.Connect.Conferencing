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
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings.Core;

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
		/// Raised when the codec sends an error.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnParsedError;

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

		private readonly Dictionary<string, IcdHashSet<ParserCallback>> m_ParserCallbacks;
		private readonly Dictionary<string, Dictionary<string, int>> m_KeyedCallbackChildren; 
		private readonly SafeCriticalSection m_ParserCallbacksSection;

		private readonly ISerialBuffer m_SerialBuffer;
		private readonly SafeTimer m_FeedbackTimer;

		private readonly ConnectionStateManager m_ConnectionStateManager;

		private readonly CiscoComponentFactory m_Components;

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
		/// The id used to register the device with the physical codec.
		/// </summary>
		public string PeripheralsId { get; private set; }

		/// <summary>
		/// Gets the phonebook to use with the directory.
		/// </summary>
		public ePhonebookType PhonebookType { get; private set; }

		/// <summary>
		/// Provides the components attached to this codec.
		/// </summary>
		public CiscoComponentFactory Components { get { return m_Components; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "The Cisco codec device"; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public CiscoCodecDevice()
		{
			m_ParserCallbacks = new Dictionary<string, IcdHashSet<ParserCallback>>();
			m_KeyedCallbackChildren = new Dictionary<string, Dictionary<string, int>>();
			m_ParserCallbacksSection = new SafeCriticalSection();

			m_ConnectionStateManager = new ConnectionStateManager(this) { ConfigurePort = ConfigurePort };
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived += PortOnSerialDataReceived;

			m_Components = new CiscoComponentFactory(this);
			m_FeedbackTimer = SafeTimer.Stopped(FeedbackTimerCallback);

			m_SerialBuffer = new XmlSerialBuffer();
			Subscribe(m_SerialBuffer);

			Controls.Add(new CiscoCodecRoutingControl(this, 0));
			Controls.Add(new CiscoCodecTraditionalConferenceControl(this, 1));
			Controls.Add(new CiscoCodecDirectoryControl(this, 2));
			Controls.Add(new CiscoCodecLayoutControl(this, 3));
			Controls.Add(new CiscoCodecPresentationControl(this, 4));
			Controls.Add(new CiscoCodecPowerControl(this, 5));
			Controls.Add(new CiscoCalendarControl(this, 6));
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
            m_ConnectionStateManager.SetPort(port);
        }
        
        /// <summary>
        /// Release resources.
        /// </summary>
        protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;
			OnConnectedStateChanged = null;
			OnParsedError = null;

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
		/// Configures a com port for communication with the hardware.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public static void ConfigureComPort(IComPort port)
		{
			port.SetComPortSpec(eComBaudRates.ComspecBaudRate115200,
			                    eComDataBits.ComspecDataBits8,
			                    eComParityType.ComspecParityNone,
			                    eComStopBits.ComspecStopBits1,
			                    eComProtocolType.ComspecProtocolRS232,
			                    eComHardwareHandshakeType.ComspecHardwareHandshakeNone,
			                    eComSoftwareHandshakeType.ComspecSoftwareHandshakeNone,
			                    false);
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

		/// <summary>
		/// Registers the given callback.
		/// Path corresponds to position in xml document, e.g. "Status", "Command", etc.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="path"></param>
		public void RegisterParserCallback(ParserCallback callback, params string[] path)
		{
			string key = XmlPathToKey(path);

			m_ParserCallbacksSection.Enter();

			try
			{
				// Callbacks
				IcdHashSet<ParserCallback> callbacks;
				if (!m_ParserCallbacks.TryGetValue(key, out callbacks))
				{
					callbacks = new IcdHashSet<ParserCallback>();
					m_ParserCallbacks.Add(key, callbacks);
				}

				callbacks.Add(callback);

				// Children
				for (int index = 1; index < path.Length - 1; index++)
				{
					string thisKey = XmlPathToKey(path.Take(index));
					string nextKey = XmlPathToKey(path.Take(index + 1));

					Dictionary<string, int> childKeys;
					if (!m_KeyedCallbackChildren.TryGetValue(thisKey, out childKeys))
					{
						childKeys = new Dictionary<string, int>();
						m_KeyedCallbackChildren.Add(thisKey, childKeys);
					}

					childKeys[nextKey] = childKeys.GetDefault(nextKey) + 1;
				}
			}
			finally
			{
				m_ParserCallbacksSection.Leave();
			}

			if (Initialized)
				RegisterFeedback(key);
		}

		/// <summary>
		/// Unregisters callbacks registered via RegisterParserCallback.
		/// </summary>
		/// <param name="callback"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool UnregisterParserCallback(ParserCallback callback, params string[] path)
		{
			if (!m_ConnectionStateManager.IsConnected)
				return false;

			string key = XmlPathToKey(path);

			m_ParserCallbacksSection.Enter();

			try
			{
				// Callbacks
				IcdHashSet<ParserCallback> callbacks;
				if (!m_ParserCallbacks.TryGetValue(key, out callbacks))
					return false;

				if (!callbacks.Remove(callback) || callbacks.Count > 0)
					return false;

				// Children
				for (int index = 1; index < path.Length - 1; index++)
				{
					string thisKey = XmlPathToKey(path.Take(index));
					string nextKey = XmlPathToKey(path.Take(index + 1));

					Dictionary<string, int> childKeys;
					if (!m_KeyedCallbackChildren.TryGetValue(thisKey, out childKeys))
						continue;

					int count;
					if (!childKeys.TryGetValue(nextKey, out count))
						continue;

					if (count > 1)
						childKeys[nextKey]--;
					else
						childKeys.Remove(nextKey);
				}
			}
			finally
			{
				m_ParserCallbacksSection.Leave();
			}

			DeregisterFeedback(key);

			return true;
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
			string[] keys = m_ParserCallbacksSection.Execute(() => m_ParserCallbacks.Keys.ToArray());

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

			Log(eSeverity.Error, message);

			OnParsedError.Raise(this, new StringEventArgs(message));
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
			IcdHashSet<string> expected =
				m_ParserCallbacksSection.Execute(() => m_ParserCallbacks.Select(p => p.Key).ToIcdHashSet());
			IcdHashSet<string> missing = expected.Subtract(actual);

			foreach (string item in missing)
			{
				Log(eSeverity.Warning, "Lost feedback on {0}, re-registering.", item);
				RegisterFeedback(item);
			}
		}

		#endregion

		#region Port Callbacks

		private void ConfigurePort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);
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

			if (args.Data)
				Initialize();
			else
			{
				Log(eSeverity.Critical, "Lost connection");
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

			// Recurse through the elements
			try
			{
				if(!StringUtils.IsNullOrWhitespace(innerXml))
					XmlUtils.Recurse(innerXml, eventArgs => XmlCallback(resultId, eventArgs));
			}
			catch (IcdXmlException e)
			{
				Log(eSeverity.Error, "Failed to parse XML - {0} - {1}", e.Message, innerXml);
			}
		}

		/// <summary>
		/// Called for each element in the xml.
		/// </summary>
		/// <param name="resultId"></param>
		/// <param name="args"></param>
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

					CallParserCallbacks(xml, resultId, key);

					//TODO: Fix this cache, for now chris says its not worth fixing
					return true;
					//Dictionary<string, int> children;
					//return m_KeyedCallbackChildren.TryGetValue(key, out children) && children.Count > 0;
			}
		}

		/// <summary>
		/// Calls the parser callbacks for the given path.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="resultId"></param>
		/// <param name="key"></param>
		private void CallParserCallbacks(string xml, string resultId, string key)
		{
			ParserCallback[] callbacks;

			m_ParserCallbacksSection.Enter();

			try
			{
				if (!m_ParserCallbacks.ContainsKey(key))
					return;

				callbacks = m_ParserCallbacks[key].ToArray();
			}
			finally
			{
				m_ParserCallbacksSection.Leave();
			}

			foreach (ParserCallback callback in callbacks)
				callback(this, resultId, xml);
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
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			PeripheralsId = null;
			PhonebookType = ePhonebookType.Corporate;

			m_ConnectionStateManager.SetPort(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(CiscoCodecSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			PeripheralsId = settings.PeripheralsId;
			PhonebookType = settings.PhonebookType;

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

			m_ConnectionStateManager.SetPort(port);
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
