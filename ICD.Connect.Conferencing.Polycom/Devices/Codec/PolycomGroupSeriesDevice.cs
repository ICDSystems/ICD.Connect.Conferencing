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
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Addressbook;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls.Calender;
using ICD.Connect.Protocol;
using ICD.Connect.Protocol.Extensions;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Settings;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec
{
	/// <summary>
	/// Model - RealPresence Group 500
	/// Hardware Version - 9
	/// System Software - Release-6.1.6.1-460003
	/// </summary>
	public sealed class PolycomGroupSeriesDevice : AbstractVideoConferenceDevice<PolycomGroupSeriesSettings>
	{
		/// <summary>
		/// End of line string.
		/// </summary>
		private const string END_OF_LINE = "\r";

		/// <summary>
		/// The number of milliseconds to wait between sending commands.
		/// </summary>
		private const long RATE_LIMIT_MS = 300;

		/// <summary>
		/// Timer interval used for resubscribing to feedbacks.
		/// </summary>
		private const int TIMER_RESUBSCRIBE_FEEDBACK_INTERVAL = 30 * 60 * 1000;

		private static readonly ComSpec s_DefaultComSpec = new ComSpec
		{
			BaudRate = eComBaudRates.BaudRate115200,
			NumberOfDataBits = eComDataBits.DataBits8,
			ParityType = eComParityType.None,
			NumberOfStopBits = eComStopBits.StopBits1,
			ProtocolType = eComProtocolType.Rs232,
			HardwareHandShake = eComHardwareHandshakeType.None,
			SoftwareHandshake = eComSoftwareHandshakeType.None,
			ReportCtsChanges = false
		};

		/// <summary>
		/// Raised when the class initializes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnInitializedChanged;

		/// <summary>
		/// Raised when the device becomes connected or disconnected.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		private readonly Dictionary<string, IcdHashSet<Action<string>>> m_FeedbackHandlers;
		private readonly Dictionary<string, IcdHashSet<Action<IEnumerable<string>>>> m_RangeFeedbackHandlers;

		private readonly RateLimitedEventQueue<string> m_CommandQueue;
		private readonly SafeTimer m_FeedbackTimer;

		private readonly PolycomComponentFactory m_Components;
		private readonly PolycomGroupSeriesSerialBuffer m_SerialBuffer;
		private readonly ConnectionStateManager m_ConnectionStateManager;

		private readonly List<string> m_CurrentMutliLines;

		private bool m_Initialized;
		private string m_CurrentMultiLineHeader;

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

				Log(eSeverity.Informational, "Initialized state changed to {0}", m_Initialized);

				OnInitializedChanged.Raise(this, new BoolEventArgs(m_Initialized));
			}
		}

		/// <summary>
		/// Gets the addressbook to use with the directory.
		/// </summary>
		public eAddressbookType AddressbookType { get; private set; }

		/// <summary>
		/// Provides the components attached to this codec.
		/// </summary>
		public PolycomComponentFactory Components
		{
			get { return m_Components; }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public PolycomGroupSeriesDevice()
		{
			m_CurrentMutliLines = new List<string>();

			m_FeedbackHandlers = new Dictionary<string, IcdHashSet<Action<string>>>();
			m_RangeFeedbackHandlers = new Dictionary<string, IcdHashSet<Action<IEnumerable<string>>>>();

			m_FeedbackTimer = new SafeTimer(ReSubscribeToFeedbacks, TIMER_RESUBSCRIBE_FEEDBACK_INTERVAL, TIMER_RESUBSCRIBE_FEEDBACK_INTERVAL);

			m_CommandQueue = new RateLimitedEventQueue<string>
			{
				BetweenMilliseconds = RATE_LIMIT_MS
			};
			m_CommandQueue.OnItemDequeued += CommandQueueOnItemDequeued;

			m_Components = new PolycomComponentFactory(this);

			m_SerialBuffer = new PolycomGroupSeriesSerialBuffer();
			Subscribe(m_SerialBuffer);

			m_ConnectionStateManager = new ConnectionStateManager(this) {ConfigurePort = ConfigurePort};
			m_ConnectionStateManager.OnConnectedStateChanged += PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged += PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived += PortOnSerialDataReceived;

			Controls.Add(new PolycomCodecRoutingControl(this, 0));
			Controls.Add(new PolycomCodecDialingControl(this, 1));
			Controls.Add(new PolycomCodecDirectoryControl(this, 2));
			Controls.Add(new PolycomCodecLayoutControl(this, 3));
			Controls.Add(new PolycomCodecPresentationControl(this, 4));
			Controls.Add(new PolycomCodecPowerControl(this, 5));
			Controls.Add(new PolycomCalendarControl(this, 6));
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

			Unsubscribe(m_SerialBuffer);

			m_FeedbackTimer.Dispose();

			base.DisposeFinal(disposing);

			m_CommandQueue.Dispose();

			m_ConnectionStateManager.OnConnectedStateChanged -= PortOnConnectionStatusChanged;
			m_ConnectionStateManager.OnIsOnlineStateChanged -= PortOnIsOnlineStateChanged;
			m_ConnectionStateManager.OnSerialDataReceived -= PortOnSerialDataReceived;
			m_ConnectionStateManager.Dispose();

			m_Components.Dispose();
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

		private void ConfigurePort(ISerialPort port)
		{
			if (port is IComPort)
				ConfigureComPort(port as IComPort);
		}

		/// <summary>
		/// Configures a com port for communication with the hardware.
		/// </summary>
		/// <param name="port"></param>
		[PublicAPI]
		public static void ConfigureComPort(IComPort port)
		{
			port.SetComPortSpec(s_DefaultComSpec);
		}

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
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		public void EnqueueCommand(string command)
		{
			EnqueueCommand(command, null);
		}

		/// <summary>
		/// Send command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="args"></param>
		public void EnqueueCommand(string command, params object[] args)
		{
			if (args != null)
				command = string.Format(command, args);

			m_CommandQueue.Enqueue(command);
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
		/// Registers the callback for handling feedback starting with the given word
		/// </summary>
		/// <param name="word"></param>
		/// <param name="callback"></param>
		public void RegisterFeedback(string word, Action<string> callback)
		{
			if (word == null)
				throw new ArgumentNullException("word");

			if (callback == null)
				throw new ArgumentNullException("callback");

			if (!m_FeedbackHandlers.ContainsKey(word))
				m_FeedbackHandlers.Add(word, new IcdHashSet<Action<string>>());

			m_FeedbackHandlers[word].Add(callback);
		}

		/// <summary>
		/// Registers the callback for handling a range of feedback
		/// 
		/// E.g.
		/// 	callinfo begin
		/// 	callinfo:43:Polycom Group Series Demo:192.168.1.101:384:connected:
		/// 	notmuted:outgoing:videocallcallinfo:36:192.168.1.102:256:connected:muted:outgoing:videocall
		/// 	callinfo end
		/// 
		/// Calls the callback for the items between the start and end
		/// </summary>
		/// <param name="word"></param>
		/// <param name="callback"></param>
		public void RegisterRangeFeedback(string word, Action<IEnumerable<string>> callback)
		{
			if (word == null)
				throw new ArgumentNullException("word");

			if (callback == null)
				throw new ArgumentNullException("callback");

			if (!m_RangeFeedbackHandlers.ContainsKey(word))
				m_RangeFeedbackHandlers.Add(word, new IcdHashSet<Action<IEnumerable<string>>>());

			m_RangeFeedbackHandlers[word].Add(callback);
		}

		public void ReSubscribeToFeedbacks()
		{
			if (!m_Initialized)
				return;

			foreach (IFeedBackComponent components in m_Components.GetComponents().OfType<IFeedBackComponent>())
			{
				components.InitializeFeedBack();
			}
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
		/// Called to send the next command to the device.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CommandQueueOnItemDequeued(object sender, GenericEventArgs<string> eventArgs)
		{
			SendCommand(eventArgs.Data);
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
			if (args.Data)
			{
				if (m_ConnectionStateManager.Port is IComPort)
					SendCommand("exit");
			}
			else
			{
				m_SerialBuffer.Clear();
				m_CommandQueue.Clear();

				ClearCurrentMultiLine();

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
		private void Subscribe(PolycomGroupSeriesSerialBuffer buffer)
		{
			buffer.OnCompletedSerial += BufferCompletedSerial;
			buffer.OnUsernamePrompt += BufferOnUsernamePrompt;
			buffer.OnPasswordPrompt += BufferOnPasswordPrompt;
		}

		/// <summary>
		/// Unsubscribe from the buffer events.
		/// </summary>
		/// <param name="buffer"></param>
		private void Unsubscribe(PolycomGroupSeriesSerialBuffer buffer)
		{
			buffer.OnCompletedSerial -= BufferCompletedSerial;
			buffer.OnUsernamePrompt -= BufferOnUsernamePrompt;
			buffer.OnPasswordPrompt -= BufferOnPasswordPrompt;
		}

		/// <summary>
		/// Called when the buffer reaches a password prompt.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void BufferOnPasswordPrompt(object sender, EventArgs eventArgs)
		{
			ISerialPort port = m_ConnectionStateManager.Port;
			if (port != null)
				port.Send(Password + END_OF_LINE);
		}

		/// <summary>
		/// Called when the buffer reaches a username prompt.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void BufferOnUsernamePrompt(object sender, EventArgs eventArgs)
		{
			ISerialPort port = m_ConnectionStateManager.Port;
			if (port != null)
				port.Send(Username + END_OF_LINE);
		}

		/// <summary>
		/// Called when the buffer completes a string.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void BufferCompletedSerial(object sender, StringEventArgs args)
		{
			string data = args.Data;

			// SSH
			if (data.StartsWith("-> "))
				data = data.Substring(3);

			data = data.Trim();

			if (string.IsNullOrEmpty(data))
				return;

			if (data.StartsWith("error:"))
				Log(eSeverity.Error, data);

			if (data.StartsWith("Hi, my name is"))
			{
				// Re-initialize every time we see the welcome message
				Initialized = false;
				Initialized = true;
			}
			else if (m_ConnectionStateManager.IsConnected)
				Initialized = true;

			HandleData(data);
		}

		private void HandleData(string data)
		{
			if (!HandleMultiLineData(data))
				HandleSingleLineData(data);
		}

		private bool HandleMultiLineData(string data)
		{
			// Handle multi-line responses
			if (data.EndsWith(" begin") || data.EndsWith(" start"))
			{
				StartNewMultiLine(data);
				return true;
			}

			if (data.EndsWith(" end"))
			{
				EndCurrentMultiLine(data);
				return true;
			}

			if (m_CurrentMultiLineHeader != null)
			{
				AppendMultiLine(data);
				return true;
			}

			return false;
		}

		private void HandleSingleLineData(string data)
		{
			// Handle one-line responses
			string word = GetFirstWord(data);
			if (word == null)
				return;

			IcdHashSet<Action<string>> handlers;
			if (!m_FeedbackHandlers.TryGetValue(word, out handlers))
				return;

			foreach (Action<string> handler in handlers.ToArray(handlers.Count))
			{
				try
				{
					handler(data);
				}
				catch (Exception e)
				{
					Log(eSeverity.Error, "Failed to handle feedback {0} - {1}", StringUtils.ToRepresentation(data), e.Message);
				}
			}
		}

		/// <summary>
		/// Gets the first word from the given response data
		/// 
		/// E.g.
		///		"autoanswer no"
		/// returns
		///		"autoanswer"
		/// 
		/// and
		///		"notification:callstatus:outgoing:3..."
		/// returns
		///		"notification"
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		[CanBeNull]
		private static string GetFirstWord(string data)
		{
			if (data == null)
				throw new ArgumentNullException("data");

			int index = data.IndexOfAny(new[] {' ', ':'});

			return index < 0 ? null : data.Substring(0, index);
		}

		#endregion

		#region Multi-Line

		private void ClearCurrentMultiLine()
		{
			m_CurrentMutliLines.Clear();
			m_CurrentMultiLineHeader = null;
		}

		private void StartNewMultiLine(string data)
		{
			ClearCurrentMultiLine();

			// Trim the " start"
			data = data.Substring(0, data.Length - " start".Length).Trim();

			m_CurrentMultiLineHeader = data;
		}

		private void EndCurrentMultiLine(string data)
		{
			if (m_CurrentMultiLineHeader == null)
				return;

			if (m_CurrentMutliLines.Count == 0)
				return;

			IcdHashSet<Action<IEnumerable<string>>> handlers;
			if (!m_RangeFeedbackHandlers.TryGetValue(m_CurrentMultiLineHeader, out handlers))
				return;

			List<string> lines = new List<string>(m_CurrentMutliLines);

			ClearCurrentMultiLine();

			foreach (Action<IEnumerable<string>> handler in handlers.ToArray(handlers.Count))
			{
				try
				{
					handler(lines);
				}
				catch (Exception e)
				{
					Log(eSeverity.Error, "Failed to handle feedback {0} - {1}", StringUtils.ToRepresentation(data), e.Message);
				}
			}
		}

		private void AppendMultiLine(string data)
		{
			m_CurrentMutliLines.Add(data);
		}

		#endregion

		#region Settings

		/// <summary>
		/// Override to apply properties to the settings instance.
		/// </summary>
		/// <param name="settings"></param>
		protected override void CopySettingsFinal(PolycomGroupSeriesSettings settings)
		{
			base.CopySettingsFinal(settings);

			settings.Port = m_ConnectionStateManager.PortNumber;
			settings.Username = Username;
			settings.Password = Password;
			settings.AddressbookType = AddressbookType;
		}

		/// <summary>
		/// Override to clear the instance settings.
		/// </summary>
		protected override void ClearSettingsFinal()
		{
			base.ClearSettingsFinal();

			Username = null;
			Password = null;
			AddressbookType = eAddressbookType.Global;

			SetPort(null);
		}

		/// <summary>
		/// Override to apply settings to the instance.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="factory"></param>
		protected override void ApplySettingsFinal(PolycomGroupSeriesSettings settings, IDeviceFactory factory)
		{
			base.ApplySettingsFinal(settings, factory);

			Username = settings.Username;
			Password = settings.Password;

			// TODO - Global addressbook not supported
			AddressbookType = eAddressbookType.Local; //settings.AddressbookType;

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

			addRow("Connected", m_ConnectionStateManager.IsOnline);
			addRow("Initialized", Initialized);
		}

		#endregion
	}
}