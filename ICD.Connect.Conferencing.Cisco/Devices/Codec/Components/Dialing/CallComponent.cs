using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing
{
	/// <summary>
	/// Call Type
	/// </summary>
	public enum eCiscoCallType
	{
		// Ignore missing comments warning
#pragma warning disable 1591
		Unknown,
		Video,
		Audio,
		AudioCanEscalate,
		ForwardAllCall
#pragma warning restore 1591

	}

	public static class CiscoCallTypeExtensions
	{
		public static eCallType ToCallType(this eCiscoCallType callType)
		{
			switch (callType)
			{
				case eCiscoCallType.Video:
					return eCallType.Video;

				case eCiscoCallType.Audio:
				case eCiscoCallType.AudioCanEscalate:
				case eCiscoCallType.ForwardAllCall:
					return eCallType.Audio;

				case eCiscoCallType.Unknown:
					return eCallType.Unknown;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	/// <summary>
	/// CallComponent represents a single call.
	/// </summary>
	public sealed class CallComponent : AbstractCiscoComponent, ITraditionalParticipant
	{
		/// <summary>
		/// Raised when the answer state changes.
		/// </summary>
		public event EventHandler<IncomingCallAnswerStateEventArgs> OnAnswerStateChanged;

		/// <summary>
		/// Raised when the call status changes.
		/// </summary>
		public event EventHandler<ParticipantStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the source name changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the source number changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnNumberChanged;

		/// <summary>
		/// Raised when the source type changes.
		/// </summary>
		public event EventHandler<ParticipantTypeEventArgs> OnParticipantTypeChanged;

		private eParticipantStatus m_Status;
		private FarCamera m_CachedCamera;
		private string m_Name;
		private string m_Number;
		private eCiscoCallType m_CiscoCallType;
		private eCallType m_SourceType;
		private eCallAnswerState m_AnswerState;

		private static readonly Dictionary<string, string> s_CachedNumberToName;
		private static readonly SafeCriticalSection s_CachedNumberToNameSection;

		// When we first attempt to establish a call it starts offline, so we track the
		// first time the call comes online and the first time afterwards the call goes
		// offline.
		private bool m_FirstOnline;
		private bool m_FirstOffline;

		#region Properties

		/// <summary>
		/// Call Answer State
		/// </summary>
		public eCallAnswerState AnswerState
		{
			get { return m_AnswerState; }
			private set
			{
				if (value == m_AnswerState)
					return;

				m_AnswerState = value;

				OnAnswerStateChanged.Raise(this, new IncomingCallAnswerStateEventArgs(m_AnswerState));
			}
		}

		/// <summary>
		/// Callback Number for the remote party
		/// </summary>
		public string Number
		{
			get { return m_Number; }
			private set
			{
				if (value == m_Number)
					return;

				m_Number = value;

				// Update the name from the cache if it hasn't been parsed yet.
				if (string.IsNullOrEmpty(Name))
					Name = GetCachedName(m_Number);
				CacheName(m_Number, Name);

				OnNumberChanged.Raise(this, new StringEventArgs(m_Number));
			}
		}

		/// <summary>
		/// Display Name of the remote party
		/// </summary>
		public string Name
		{
			get { return m_Name; }
			private set
			{
				if (value == m_Name)
					return;

				m_Name = value;

				// Update the name in the cache
				if (!string.IsNullOrEmpty(Number))
					CacheName(Number, m_Name);

				OnNameChanged.Raise(this, new StringEventArgs(m_Name));
			}
		}

		/// <summary>
		/// Call Direction
		/// </summary>
		public eCallDirection Direction { get; private set; }

		/// <summary>
		/// The time the call started.
		/// </summary>
		public DateTime? Start { get; private set; }

		/// <summary>
		/// The time the call ended.
		/// </summary>
		public DateTime? End { get; private set; }

		public DateTime DialTime { get; private set; }

		/// <summary>
		/// Call Id
		/// </summary>
		public int CallId { get; private set; }

		/// <summary>
		/// Protocol for call
		/// </summary>
		[PublicAPI]
		public string Protocol { get; private set; }

		/// <summary>
		/// Receive rate for the call in kbps
		/// </summary>
		[PublicAPI]
		public int ReceiveRate { get; private set; }

		/// <summary>
		/// Call Remote Number for remote party
		/// </summary>
		[PublicAPI]
		public string RemoteNumber { get; private set; }

		/// <summary>
		/// Call Status
		/// </summary>
		public eParticipantStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (value == m_Status)
					return;

				m_Status = value;
				Codec.Log(eSeverity.Informational, "Call {0} status changed: {1}", CallId, StringUtils.NiceName(m_Status));

				UpdateIsOnlineStatus();

				OnStatusChanged.Raise(this, new ParticipantStatusEventArgs(m_Status));
			}
		}

		/// <summary>
		/// Transmit rate for the call in kbps
		/// </summary>
		[PublicAPI]
		public int TransmitRate { get; private set; }

		/// <summary>
		/// Call Type
		/// </summary>
		[PublicAPI]
		public eCiscoCallType CiscoCallType
		{
			get { return m_CiscoCallType; }
			private set
			{
				if (value == m_CiscoCallType)
					return;

				m_CiscoCallType = value;

				CallType = m_CiscoCallType.ToCallType();
			}
		}

		/// <summary>
		/// Gets the source type.
		/// </summary>
		public eCallType CallType
		{
			get { return m_SourceType; }
			private set
			{
				if (value == m_SourceType)
					return;

				m_SourceType = value;

				OnParticipantTypeChanged.Raise(this, new ParticipantTypeEventArgs(m_SourceType));
			}
		}

		/// <summary>
		/// Gets the remote camera for this call.
		/// </summary>
		[PublicAPI]
		public FarCamera Camera
		{
			get
			{
				if (CallType != eCallType.Video)
					return null;
				return m_CachedCamera ?? (m_CachedCamera = new FarCamera(CallId, Codec));
			}
		}

		IRemoteCamera IParticipant.Camera { get { return Camera; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Static constructor.
		/// </summary>
		static CallComponent()
		{
			s_CachedNumberToName = new Dictionary<string, string>();
			s_CachedNumberToNameSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Call Id
		/// </summary>
		/// <param name="callId"></param>
		/// <param name="codec"></param>
		public CallComponent(int callId, CiscoCodecDevice codec) : base(codec)
		{
			CallId = callId;

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();

			DialTime = IcdEnvironment.GetLocalTime();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			OnAnswerStateChanged = null;
			OnStatusChanged = null;
			OnNameChanged = null;
			OnParticipantTypeChanged = null;
			OnNumberChanged = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Updates the properties to match the status xml.
		/// </summary>
		/// <param name="xml"></param>
		public void Parse(string xml)
		{
			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.ReadToNextElement();

				int callId = reader.GetAttributeAsInt("item");
				if (callId != CallId)
					return;

				// Dead calls rise up as ghosts.
				bool ghost = reader.GetAttribute("ghost") == "True";
				if (ghost)
					Status = eParticipantStatus.Disconnected;

				foreach (IcdXmlReader child in reader.GetChildElements())
				{
					switch (child.Name)
					{
						case "Status":
							SetStatus(child.ReadElementContentAsString());
							break;
						case "Direction":
							SetDirection(child.ReadElementContentAsString());
							break;
						case "Protocol":
							Protocol = child.ReadElementContentAsString();
							break;
						case "CallType":
							SetCallType(child.ReadElementContentAsString());
							break;
						case "RemoteNumber":
							RemoteNumber = child.ReadElementContentAsString();
							break;
						case "CallbackNumber":
							Number = child.ReadElementContentAsString();
							break;
						case "DisplayName":
							Name = child.ReadElementContentAsString();
							break;
						case "TransmitCallRate":
							TransmitRate = child.ReadElementContentAsInt();
							break;
						case "ReceiveCallRate":
							ReceiveRate = child.ReadElementContentAsInt();
							break;
						case "AnswerState":
							SetAnswerState(child.ReadElementContentAsString());
							break;
						case "Duration":
							SetDuration(child.ReadElementContentAsInt());
							break;
					}

					child.Dispose();
				}
			}
		}

		/// <summary>
		/// Answers the incoming call.
		/// </summary>
		public void Answer()
		{
			Codec.SendCommand("xCommand Call Accept CallId: {0}", CallId);
			Codec.Log(eSeverity.Debug, "Answering Incoming Call {0}", CallId);
		}

		/// <summary>
		/// Rejects the incoming call.
		/// </summary>
		public void Reject()
		{
			Codec.SendCommand("xCommand Call Reject CallId: {0}", CallId);
			Codec.Log(eSeverity.Debug, "Rejecting Incoming Call {0}", CallId);
		}

		/// <summary>
		/// Holds the call.
		/// </summary>
		public void Hold()
		{
			if (Status == eParticipantStatus.OnHold)
				return;

			Codec.SendCommand("xCommand Call Hold CallId: {0}", CallId);
			Codec.Log(eSeverity.Debug, "Placing Call {0} on hold", CallId);
		}

		/// <summary>
		/// Resumes the call.
		/// </summary>
		public void Resume()
		{
			if (Status != eParticipantStatus.OnHold)
				return;

			Codec.SendCommand("xCommand Call Resume CallId: {0}", CallId);
			Codec.Log(eSeverity.Debug, "Resuming Call {0}", CallId);
		}

		/// <summary>
		/// Disconnects all calls.
		/// </summary>
		public void Hangup()
		{
			Codec.SendCommand("xCommand Call Disconnect CallId: {0}", CallId);
			Codec.Log(eSeverity.Debug, "Disconnecting Call {0}", CallId);
		}

		/// <summary>
		/// Combines this call and the call with the given id.
		/// </summary>
		/// <param name="other"></param>
		public void Join(int other)
		{
			Codec.SendCommand("xCommand Call Join CallId: {0} CallId: {1}", CallId, other);
			Codec.Log(eSeverity.Debug, "Joining Call {0} with {1}", CallId, other);
		}

		/// <summary>
		/// Allows sending data to dial-tone menus.
		/// </summary>
		/// <param name="data"></param>
		public void SendDtmf(string data)
		{
			Codec.SendCommand("xCommand Call DTMFSend CallId: {0} DTMFString: \"{1}\"", CallId, data);
			Codec.Log(eSeverity.Debug, "Sending DTMF tone {0} to call {1}", data, CallId);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Updates the call when it goes offline or comes online.
		/// </summary>
		private void UpdateIsOnlineStatus()
		{
			bool isOnline = this.GetIsOnline();

			// If we came online for the first time, update the call start.
			if (!m_FirstOnline && isOnline)
			{
				if (Start == null)
					Start = IcdEnvironment.GetLocalTime();
				m_FirstOnline = true;
			}

			// If we went offline for the first time, update the call end.
			if (m_FirstOnline && !m_FirstOffline && !isOnline)
			{
				if (End == null)
					End = IcdEnvironment.GetLocalTime();
				m_FirstOffline = true;
			}
		}

		/// <summary>
		/// Called when the component connects/disconnects to the codec.
		/// </summary>
		protected override void ConnectionStatusChanged(bool state)
		{
			base.ConnectionStatusChanged(state);

			if (!state)
				Status = eParticipantStatus.Disconnected;
		}

		/// <summary>
		/// Sets the cached name for the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="name"></param>
		private static void CacheName(string number, string name)
		{
			s_CachedNumberToNameSection.Execute(() => s_CachedNumberToName[number] = name);
		}

		/// <summary>
		/// Gets the cached name for the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		private static string GetCachedName(string number)
		{
			return s_CachedNumberToNameSection.Execute(() => s_CachedNumberToName.GetDefault(number, string.Empty));
		}

		#endregion

		#region Codec Callbacks

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseCallStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Call");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseCallStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Call");
		}

		private void ParseCallStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			Parse(xml);
		}

		private void SetCallType(string callType)
		{
			CiscoCallType = EnumUtils.Parse<eCiscoCallType>(callType, true);
		}

		private void SetDirection(string direction)
		{
			Direction = EnumUtils.Parse<eCallDirection>(direction, true);
		}

		private void SetAnswerState(string state)
		{
			AnswerState = EnumUtils.Parse<eCallAnswerState>(state, true);
		}

		private void SetStatus(string status)
		{
			// Codec uses English spelling.
			if (status.ToLower() == "dialling")
				status = "dialing";

			Status = EnumUtils.Parse<eParticipantStatus>(status, true);
		}

		/// <summary>
		/// Handles a parsed duration.
		/// </summary>
		/// <param name="duration"></param>
		private void SetDuration(int duration)
		{
			DateTime end = End ?? IcdEnvironment.GetLocalTime();
			Start = end - TimeSpan.FromSeconds(duration);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Hold", "Holds the call", () => Hold());
			yield return new ConsoleCommand("Resume", "Resumes the call", () => Resume());
			yield return new ConsoleCommand("Hangup", "Ends the call", () => Hangup());
			yield return new ConsoleCommand("Answer", "Answers the incoming call", () => Answer());
			yield return new ConsoleCommand("Reject", "Rejects the incoming call", () => Reject());
			yield return new GenericConsoleCommand<string>("SendDTMF", "SendDTMF x", s => SendDtmf(s));
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Gets the child console node groups.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase group in GetBaseConsoleNodes())
				yield return group;

			if (CallType == eCallType.Video)
				yield return Camera;
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
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

			addRow("Call ID", CallId);
			addRow("Status", Status);
			addRow("Direction", Direction);
			addRow("Protocol", Protocol);
			addRow("Call Type", CiscoCallType);
			addRow("Remote Number", RemoteNumber);
			addRow("Callback Number", Number);
			addRow("Name", Name);
			addRow("Transmit Call Rate", TransmitRate);
			addRow("Receive Call Rate", ReceiveRate);
			addRow("Duration", this.GetDuration());
			addRow("Answer State", AnswerState);
		}

		#endregion
	}
}
