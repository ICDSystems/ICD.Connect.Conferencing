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
		private const string ELEMENT_AUTHENTICATION_REQUEST = "AuthenticationRequest";

		#endregion

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
		public event EventHandler<GenericEventArgs<eAuthenticationRequest>> OnAuthenticationRequestChanged; 

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
		private eAuthenticationRequest m_AuthenticationRequest;

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

		/// <summary>
		/// Authentication request for the call
		/// </summary>
		public eAuthenticationRequest AuthenticationRequest
		{
			get { return m_AuthenticationRequest; }
			private set
			{
				if (m_AuthenticationRequest == value)
					return;

				m_AuthenticationRequest = value;
				
				OnAuthenticationRequestChanged.Raise(this, m_AuthenticationRequest);
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
		/// <param name="authenticationRequest"></param>
		private CallStatus(eCallAnswerState answerState, string number, string name, eCallDirection direction, int duration, int callId,
		                   eCiscoDialProtocol protocol, int receiveRate, string remoteNumber, eCiscoCallStatus status, int transmitRate,
		                   eCiscoCallType ciscoCallType, eAuthenticationRequest authenticationRequest)
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
			m_AuthenticationRequest = authenticationRequest;
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

				eCallAnswerState answerState = eCallAnswerState.Unknown;
				string number = null;
				string name = null;
				eCallDirection direction = eCallDirection.Undefined;
				eCiscoDialProtocol protocol = eCiscoDialProtocol.Unknown;
				int receiveRate = 0;
				string remoteNumber = null;
				eCiscoCallStatus status = eCiscoCallStatus.Undefined;
				int transmitRate = 0;
				eCiscoCallType ciscoCallType = eCiscoCallType.Unknown;
				int duration = 0;
				eAuthenticationRequest authenticationRequest = eAuthenticationRequest.Unknown;

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
						case ELEMENT_AUTHENTICATION_REQUEST:
							authenticationRequest = EnumUtils.Parse<eAuthenticationRequest>(child.ReadElementContentAsString(), true);
							break;
					}

					child.Dispose();
				}

				// Static calculations
				status = ghost ? eCiscoCallStatus.Disconnected : status;

				return new CallStatus(answerState, number, name, direction, duration, callId, protocol,
				                      receiveRate, remoteNumber, status, transmitRate,
				                      ciscoCallType, authenticationRequest);
			}
		}

		#endregion

		#region Methods

        public void SetOrphaned()
        {
            Status = eCiscoCallStatus.Orphaned;
        }

		/// <summary>
		/// Updates the values based on new xml.
		/// </summary>
		/// <param name="xml"></param>
		public void UpdateFromXml(string xml)
		{
			var updated = FromXml(xml);
			if (updated.CallId != CallId)
				return;

			AnswerState = updated.AnswerState != eCallAnswerState.Unknown ? updated.AnswerState : AnswerState;
			Number = updated.Number ?? Number;
			Name = updated.Name ?? Name;
			Direction = updated.Direction != eCallDirection.Undefined ? updated.Direction : Direction;
			Duration = updated.Duration != 0 ? updated.Duration : Duration;
			Protocol = updated.Protocol != eCiscoDialProtocol.Unknown ? updated.Protocol : Protocol;
			ReceiveRate = updated.ReceiveRate != 0 ? updated.ReceiveRate : ReceiveRate;
			RemoteNumber = updated.RemoteNumber ?? RemoteNumber;
			Status = updated.Status != eCiscoCallStatus.Undefined ? updated.Status : Status;
			TransmitRate = updated.TransmitRate != 0 ? updated.TransmitRate : TransmitRate;
			CiscoCallType = updated.CiscoCallType != eCiscoCallType.Unknown ? updated.CiscoCallType : CiscoCallType;
			AuthenticationRequest = updated.AuthenticationRequest != eAuthenticationRequest.Unknown
				? updated.AuthenticationRequest
				: AuthenticationRequest;
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