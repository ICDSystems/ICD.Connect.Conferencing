using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing
{
	public sealed class CallStatus : IConsoleNode
	{
		#region Xml Constants

		private const string ATTRIBUTE_CALL_ID = "item";
		private const string ATTRIBUTE_GHOST = "ghost";
		private const string ELEMENT_ANSWER_STATE = "AnswerState";
		private const string ELEMENT_CALLBACK_NUMBER = "CallbackNumber";
		private const string ELEMENT_DIRECTION = "Direction";
		private const string ELEMENT_DISPLAY_NAME = "DisplayName";
		private const string ELEMENT_DURATION = "Duration";
		private const string ELEMENT_PROTOCOL = "Protocol";
		private const string ELEMENT_RECEIVE_CALL_RATE = "ReceiveCallRate";
		private const string ELEMENT_REMOTE_NUMBER = "RemoteNumber";
		private const string ELEMENT_STATUS = "Status";
		private const string ELEMENT_TRANSMIT_CALL_RATE = "TransmitCallRate";
		private const string ELEMENT_CALL_TYPE = "CallType";

		#endregion
		
		private const eCallAnswerState DEFAULT_ANSWER_STATE = eCallAnswerState.Unknown;
		private const string DEFAULT_NUMBER = null;
		private const string DEFAULT_NAME = null;
		private const eCallDirection DEFAULT_DIRECTION = eCallDirection.Undefined;
		private const eCiscoDialProtocol DEFAULT_PROTOCOL = eCiscoDialProtocol.Unknown;
		private const int DEFAULT_RECEIVE_RATE = 0;
		private const string DEFAULT_REMOTE_NUMBER = null;
		private const eCiscoCallStatus DEFAULT_STATUS = eCiscoCallStatus.Undefined;
		private const int DEFAULT_TRANSMIT_RATE = 0;
		private const eCiscoCallType DEFAULT_CISCO_CALL_TYPE = eCiscoCallType.Unknown;
		private const int DEFAULT_DURATION = 0;

		#region Events

		public event EventHandler<GenericEventArgs<eCallAnswerState>> OnAnswerStateChanged;
		public event EventHandler<StringEventArgs> OnNumberChanged;
		public event EventHandler<StringEventArgs> OnNameChanged;
		public event EventHandler<GenericEventArgs<eCallDirection>> OnDirectionChanged;
		public event EventHandler<IntEventArgs> OnDurationChanged;
		public event EventHandler<IntEventArgs> OnCallIdChanged;
		public event EventHandler<GenericEventArgs<eCiscoDialProtocol>> OnProtocolChanged;
		public event EventHandler<IntEventArgs> OnReceiveRateChanged;
		public event EventHandler<StringEventArgs> OnRemoteNumberChanged;
		public event EventHandler<GenericEventArgs<eCiscoCallStatus>> OnStatusChanged;
		public event EventHandler<IntEventArgs> OnTransmitRateChanged;
		public event EventHandler<GenericEventArgs<eCiscoCallType>> OnCiscoCallTypeChanged;

		#endregion

		#region Fields

		private eCallAnswerState m_AnswerState;
		private string m_Number;
		private string m_Name;
		private eCallDirection m_Direction;
		private int m_Duration;
		private int m_CallId;
		private eCiscoDialProtocol m_Protocol;
		private int m_ReceiveRate;
		private string m_RemoteNumber;
		private eCiscoCallStatus m_Status;
		private int m_TransmitRate;
		private eCiscoCallType m_CiscoCallType;

		private static readonly Dictionary<string, string> s_CachedNumberToName;
		private static readonly SafeCriticalSection s_CachedNumberToNameSection;

		#endregion

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

				OnAnswerStateChanged.Raise(this, m_AnswerState);
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
				if (string.IsNullOrEmpty(Name) && m_Number != null)
					Name = GetCachedName(m_Number);
				CacheName(m_Number, Name);

				OnNumberChanged.Raise(this, m_Number);
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

				OnNameChanged.Raise(this, m_Name);
			}
		}

		/// <summary>
		/// Call Direction
		/// </summary>
		public eCallDirection Direction
		{
			get { return m_Direction; }
			private set
			{
				if (value == m_Direction)
					return;

				m_Direction = value;

				OnDirectionChanged.Raise(this, m_Direction);
			}
		}

		/// <summary>
		/// The duration of the call in seconds.
		/// </summary>
		public int Duration
		{
			get { return m_Duration; }
			private set
			{
				if (value == m_Duration)
					return;

				m_Duration = value;

				OnDurationChanged.Raise(this, m_Duration);
			}
		}

		/// <summary>
		/// Call Id
		/// </summary>
		public int CallId
		{
			get { return m_CallId; }
			private set
			{
				if (value == m_CallId)
					return;

				m_CallId = value;

				OnCallIdChanged.Raise(this, m_CallId);
			}
		}

		/// <summary>
		/// Protocol for call
		/// </summary>
		public eCiscoDialProtocol Protocol
		{
			get { return m_Protocol; }
			private set
			{
				if (value == m_Protocol)
					return;

				m_Protocol = value;

				OnProtocolChanged.Raise(this, m_Protocol);
			}
		}

		/// <summary>
		/// Receive rate for the call in kbps
		/// </summary>
		public int ReceiveRate
		{
			get { return m_ReceiveRate; }
			private set
			{
				if (value == m_ReceiveRate)
					return;

				m_ReceiveRate = value;

				OnReceiveRateChanged.Raise(this, m_ReceiveRate);
			}
		}

		/// <summary>
		/// Call Remote Number for remote party
		/// </summary>
		public string RemoteNumber
		{
			get { return m_RemoteNumber; }
			private set
			{
				if (value == m_RemoteNumber)
					return;

				m_RemoteNumber = value;

				OnRemoteNumberChanged.Raise(this, m_RemoteNumber);
			}
		}

		/// <summary>
		/// Call Status
		/// </summary>
		public eCiscoCallStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (value == m_Status)
					return;

				m_Status = value;

				OnStatusChanged.Raise(this, m_Status);
			}
		}

		/// <summary>
		/// Transmit rate for the call in kbps
		/// </summary>
		public int TransmitRate
		{
			get { return m_TransmitRate; }
			private set
			{
				if (value == m_TransmitRate)
					return;

				m_TransmitRate = value;

				OnTransmitRateChanged.Raise(this, m_TransmitRate);
			}
		}

		/// <summary>
		/// Gets the source type
		/// </summary>
		public eCiscoCallType CiscoCallType
		{
			get { return m_CiscoCallType; }
			private set
			{
				if (value == m_CiscoCallType)
					return;

				m_CiscoCallType = value;

				OnCiscoCallTypeChanged.Raise(this, m_CiscoCallType);
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Static constructor.
		/// </summary>
		static CallStatus()
		{
			s_CachedNumberToName = new Dictionary<string, string>();
			s_CachedNumberToNameSection = new SafeCriticalSection();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="answerState"></param>
		/// <param name="number"></param>
		/// <param name="name"></param>
		/// <param name="direction"></param>
		/// <param name="duration"></param>
		/// <param name="callId"></param>
		/// <param name="protocol"></param>
		/// <param name="receiveRate"></param>
		/// <param name="remoteNumber"></param>
		/// <param name="status"></param>
		/// <param name="transmitRate"></param>
		/// <param name="ciscoCallType"></param>
		private CallStatus(eCallAnswerState answerState, string number, string name, eCallDirection direction, int duration, int callId,
		                   eCiscoDialProtocol protocol, int receiveRate, string remoteNumber, eCiscoCallStatus status, int transmitRate,
		                   eCiscoCallType ciscoCallType)
		{
			m_AnswerState = answerState;
			m_Number = number;
			m_Name = name;
			m_Direction = direction;
			m_Duration = duration;
			m_CallId = callId;
			m_Protocol = protocol;
			m_ReceiveRate = receiveRate;
			m_RemoteNumber = remoteNumber;
			m_Status = status;
			m_TransmitRate = transmitRate;
			m_CiscoCallType = ciscoCallType;
		}

		/// <summary>
		/// Parses a call status from xml.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static CallStatus FromXml(string xml)
		{
			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.ReadToNextElement();

				int callId = reader.GetAttributeAsInt(ATTRIBUTE_CALL_ID);
				bool ghost = reader.GetAttribute(ATTRIBUTE_GHOST) == "True";

				eCallAnswerState answerState = DEFAULT_ANSWER_STATE;
				string number = DEFAULT_NUMBER;
				string name = DEFAULT_NAME;
				eCallDirection direction = DEFAULT_DIRECTION;
				eCiscoDialProtocol protocol = DEFAULT_PROTOCOL;
				int receiveRate = DEFAULT_RECEIVE_RATE;
				string remoteNumber = DEFAULT_REMOTE_NUMBER;
				eCiscoCallStatus status = DEFAULT_STATUS;
				int transmitRate = DEFAULT_TRANSMIT_RATE;
				eCiscoCallType ciscoCallType = DEFAULT_CISCO_CALL_TYPE;
				int duration = DEFAULT_DURATION;

				foreach (IcdXmlReader child in reader.GetChildElements())
				{
					switch (child.Name)
					{
						case ELEMENT_ANSWER_STATE:
							answerState = EnumUtils.Parse<eCallAnswerState>(child.ReadElementContentAsString(), true);
							break;
						case ELEMENT_CALLBACK_NUMBER:
							number = child.ReadElementContentAsString();
							break;
						case ELEMENT_DISPLAY_NAME:
							name = child.ReadElementContentAsString();
							break;
						case ELEMENT_DIRECTION:
							direction = EnumUtils.Parse<eCallDirection>(child.ReadElementContentAsString(), true);
							break;
						case ELEMENT_PROTOCOL:
							protocol = EnumUtils.Parse<eCiscoDialProtocol>(child.ReadElementContentAsString(), true);
							break;
						case ELEMENT_RECEIVE_CALL_RATE:
							receiveRate = child.ReadElementContentAsInt();
							break;
						case ELEMENT_REMOTE_NUMBER:
							remoteNumber = child.ReadElementContentAsString();
							break;
						case ELEMENT_STATUS:
							status = EnumUtils.Parse<eCiscoCallStatus>(ParseStatusElement(child.ReadElementContentAsString()), true);
							break;
						case ELEMENT_TRANSMIT_CALL_RATE:
							transmitRate = child.ReadElementContentAsInt();
							break;
						case ELEMENT_CALL_TYPE:
							ciscoCallType = EnumUtils.Parse<eCiscoCallType>(child.ReadElementContentAsString(), true);
							break;
						case ELEMENT_DURATION:
							duration = child.ReadElementContentAsInt();
							break;
					}

					child.Dispose();
				}

				// Static calculations
				status = ghost ? eCiscoCallStatus.Disconnected : status;

				return new CallStatus(answerState, number, name, direction, duration, callId, protocol,
				                      receiveRate, remoteNumber, status, transmitRate,
				                      ciscoCallType);
			}
		}

		#endregion

		#region Methods

        public void SetOrphaned()
        {
            Status = eCiscoCallStatus.Orphaned;
        }

        private void UpdateFromCallStatus(CallStatus updated)
        {
	        if (updated.CallId != CallId)
		        return;

	        AnswerState = updated.AnswerState != DEFAULT_ANSWER_STATE ? updated.AnswerState : AnswerState;
	        Number = updated.Number ?? Number;
	        Name = updated.Name ?? Name;
	        Direction = updated.Direction != DEFAULT_DIRECTION ? updated.Direction : Direction;
	        Duration = updated.Duration != DEFAULT_DURATION ? updated.Duration : Duration;
	        Protocol = updated.Protocol != DEFAULT_PROTOCOL ? updated.Protocol : Protocol;
	        ReceiveRate = updated.ReceiveRate != DEFAULT_RECEIVE_RATE ? updated.ReceiveRate : ReceiveRate;
	        RemoteNumber = updated.RemoteNumber ?? RemoteNumber;
	        Status = updated.Status != DEFAULT_STATUS ? updated.Status : Status;
	        TransmitRate = updated.TransmitRate != DEFAULT_TRANSMIT_RATE ? updated.TransmitRate : TransmitRate;
	        CiscoCallType = updated.CiscoCallType != DEFAULT_CISCO_CALL_TYPE ? updated.CiscoCallType : CiscoCallType;
        }

		/// <summary>
		/// Updates the values based on new xml.
		/// </summary>
		/// <param name="xml"></param>
		public void UpdateFromXml(string xml)
		{
			var updated = FromXml(xml);
			UpdateFromCallStatus(updated);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Format dialing (dialling) status case for enum parsing.
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		private static string ParseStatusElement(string element)
		{
			// Codec uses English spelling.
			if (element.ToLower() == "dialling")
				element = "dialing";

			return element;
		}

		/// <summary>
		/// Sets the cached name for the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="name"></param>
		private static void CacheName(string number, string name)
		{
			// Don't cache null keys.
			if (number == null)
				return;

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

		#region Console

		public string ConsoleName { get { return string.Format("Call {0} Status", CallId); } }
		public string ConsoleHelp { get { return "Information about the call"; } }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Call ID", CallId);
			addRow("Answer State", AnswerState);
			addRow("Status", Status);
			addRow("Direction", Direction);
			addRow("Protocol", Protocol);
			addRow("Call Type", CiscoCallType);
			addRow("Remote Number", RemoteNumber);
			addRow("Callback Number", Number);
			addRow("Name", Name);
			addRow("Transmit Call Rate", TransmitRate);
			addRow("Receive Call Rate", ReceiveRate);
			addRow("Duration", Duration);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}