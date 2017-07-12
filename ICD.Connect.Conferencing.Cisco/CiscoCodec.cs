using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cisco.Components;
using ICD.Connect.Conferencing.Cisco.Controls;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.SerialBuffers;
using ICD.Connect.Settings.Core;

namespace ICD.Connect.Conferencing.Cisco
{
	/// <summary>
	/// Cisco VTC Codec Control
	/// </summary>
	public sealed class CiscoCodec : AbstractDevice<CiscoCodecSettings>
	{
		/// <summary>
		/// Callback for parser events.
		/// </summary>
		/// <param name="codec"></param>
		/// <param name="resultId"></param>
		/// <param name="xml"></param>
		public delegate void ParserCallback(CiscoCodec codec, string resultId, string xml);

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

		private readonly Dictionary<string, List<ParserCallback>> m_ParserCallbacks;
		private readonly SafeCriticalSection m_ParserCallbacksSection;

		private readonly ISerialBuffer m_SerialBuffer;
		private readonly SafeTimer m_FeedbackTimer;

		private readonly CiscoComponentFactory m_Components;

		private bool m_Initialized;
		private bool m_IsConnected;
		private ISerialPort m_Port;

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
		/// Provides the components attached to this codec.
		/// </summary>
		public CiscoComponentFactory Components { get { return m_Components; } }

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

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public override string ConsoleHelp { get { return "The Cisco codec device"; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public CiscoCodec()
		{
			m_ParserCallbacks = new Dictionary<string, List<ParserCallback>>();
			m_ParserCallbacksSection = new SafeCriticalSection();

			m_Components = new CiscoComponentFactory(this);
			m_FeedbackTimer = SafeTimer.Stopped(FeedbackTimerCallback);

			m_SerialBuffer = new XmlSerialBuffer();
			Subscribe(m_SerialBuffer);

			Controls.Add(new CiscoCodecRoutingControl(this, 0));
			Controls.Add(new CiscoDialingDeviceControl(this, 1));
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal(bool disposing)
		{
			OnInitializedChanged = null;
			OnConnectedStateChanged = null;
			OnParsedError = null;

			base.DisposeFinal(disposing);

			m_FeedbackTimer.Dispose();

			Unsubscribe(m_Port);
			Disconnect();
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

			Unsubscribe(m_Port);
			m_Port = port;
			Subscribe(m_Port);

			UpdateCachedOnlineStatus();
		}

		/// <summary>
		/// Connect to the codec.
		/// </summary>
		[PublicAPI]
		public void Connect()
		{
			if (m_Port == null)
			{
				Log(eSeverity.Critical, "Unable to connect, port is null");
				return;
			}

			m_Port.Connect();
			IsConnected = m_Port.IsConnected;
		}

		/// <summary>
		/// Disconnect from the codec.
		/// </summary>
		[PublicAPI]
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
		/// Puts the codec in standby mode.
		/// </summary>
		[PublicAPI]
		public void Sleep()
		{
			SendCommand("xCommand Standby Activate");
		}

		/// <summary>
		/// Wakes the codec from standby.
		/// </summary>
		[PublicAPI]
		public void Wake()
		{
			SendCommand("xCommand Standby Deactivate");
		}

		/// <summary>
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		public void SendCommand(string command)
		{
			SendCommand(command, new object[0]);
		}

		/// <summary>
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		public void SendCommand(string command, params object[] args)
		{
			command = string.Format(command, args);

			if (!IsConnected)
			{
				Log(eSeverity.Warning, "Codec is disconnected, attempting reconnect");
				Connect();
			}

			if (!IsConnected)
			{
				Log(eSeverity.Critical, "Unable to communicate with Codec");
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
			m_ParserCallbacksSection.Enter();

			try
			{
				string key = XmlPathToKey(path);

				if (!m_ParserCallbacks.ContainsKey(key))
					m_ParserCallbacks[key] = new List<ParserCallback>();

				m_ParserCallbacks[key].Add(callback);

				if (Initialized)
					RegisterFeedback(key);
			}
			finally
			{
				m_ParserCallbacksSection.Leave();
			}
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
			m_ParserCallbacksSection.Enter();

			try
			{
				string key = XmlPathToKey(path);

				if (!m_ParserCallbacks.ContainsKey(key))
					return false;

				bool output = m_ParserCallbacks[key].Remove(callback);
				if (output && m_ParserCallbacks[key].Count == 0)
					DeregisterFeedback(key);

				return output;
			}
			finally
			{
				m_ParserCallbacksSection.Leave();
			}
		}

		/// <summary>
		/// Logs the message.
		/// </summary>
		/// <param name="severity"></param>
		/// <param name="message"></param>
		/// <param name="args"></param>
		public void Log(eSeverity severity, string message, params object[] args)
		{
			message = string.Format(message, args);

			ServiceProvider.GetService<ILoggerService>().AddEntry(severity, AddLogPrefix(message));
		}

		/// <summary>
		/// Returns the log message with a CiscoCodec prefix.
		/// </summary>
		/// <param name="log"></param>
		/// <returns></returns>
		private string AddLogPrefix(string log)
		{
			return string.Format("{0} - {1}", GetType().Name, log);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current online status of the device.
		/// </summary>
		/// <returns></returns>
		protected override bool GetIsOnlineStatus()
		{
			return m_Port != null && m_Port.IsOnline;
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
		/// Calls the parser callbacks for the given path.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="resultId"></param>
		/// <param name="path"></param>
		private void CallParserCallbacks(string xml, string resultId, IEnumerable<string> path)
		{
			string key = XmlPathToKey(path);
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
		/// Called for each element in the xml.
		/// </summary>
		/// <param name="resultId"></param>
		/// <param name="args"></param>
		private void XmlCallback(string resultId, XmlRecursionEventArgs args)
		{
			string xml = args.Outer;

			CallParserCallbacks(xml, resultId, args.Path.Skip(1));

			// Check for errors
			IcdXmlAttribute statusAttr;

			try
			{
				statusAttr = XmlUtils.GetAttribute(xml, "status");
			}
			catch (InvalidOperationException)
			{
				return;
			}

			if (statusAttr.Value == "Error")
				ParseError(xml);
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
				reader.SkipToNextElement();

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
		/// Periodically re-register feedback to make sure nothing myseriously vanishes.
		/// </summary>
		private void FeedbackTimerCallback()
		{
			if (IsConnected)
				SendCommand("xFeedback List | resultId=\"{0}\"", FEEDBACK_TIMER_CALLBACK_ID);
		}

		/// <summary>
		/// Parse the feedback registration and ensure nothing has been lost.
		/// </summary>
		/// <param name="xml"></param>
		private void ParseFeedbackRegistration(string xml)
		{
			string inner;
			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.SkipToNextElement();
				inner = reader.ReadElementContentAsString();
			}

			IcdHashSet<string> actual = new IcdHashSet<string>(inner.Split());
			IcdHashSet<string> expected =
				m_ParserCallbacksSection.Execute(() => new IcdHashSet<string>(m_ParserCallbacks.Select(p => p.Key)));
			IcdHashSet<string> missing = expected.Subtract(actual);

			foreach (string item in missing)
			{
				Log(eSeverity.Warning, "Lost feedback on {0}, re-registering.", item);
				RegisterFeedback(item);
			}
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
		/// <param name="boolEventArgs"></param>
		private void PortOnIsOnlineStateChanged(object sender, BoolEventArgs boolEventArgs)
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
		/// Called when the buffer completes a string.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void SerialBufferCompletedSerial(object sender, StringEventArgs args)
		{
			string xml = args.Data;

			// Pull the resultId from the xml
			string resultId;
			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.SkipToNextElement();
				resultId = reader.GetAttribute("resultId");
			}

			// Parse feedback registration
			if (resultId == FEEDBACK_TIMER_CALLBACK_ID)
			{
				ParseFeedbackRegistration(xml);
				return;
			}

			// Recurse through the elements
			XmlUtils.Recurse(xml, eventArgs => XmlCallback(resultId, eventArgs));
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

			settings.Port = m_Port == null ? (int?)null : m_Port.Id;
			settings.PeripheralsId = PeripheralsId;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			PeripheralsId = null;
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

			PeripheralsId = settings.PeripheralsId;

			ISerialPort port = null;

			if (settings.Port != null)
			{
				port = factory.GetPortById((int)settings.Port) as ISerialPort;
				if (port == null)
					Logger.AddEntry(eSeverity.Error, "No serial port with id {0}", settings.Port);
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

			foreach (IConsoleNodeBase node in m_Components.GetComponents()
			                                              .OrderBy(c => c.GetType().Name)
			                                              .Cast<IConsoleNodeBase>())
				yield return node;
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

			addRow("Connected", IsConnected);
			addRow("Initialized", Initialized);
			addRow("Peripherals ID", PeripheralsId);
		}

		#endregion
	}
}
