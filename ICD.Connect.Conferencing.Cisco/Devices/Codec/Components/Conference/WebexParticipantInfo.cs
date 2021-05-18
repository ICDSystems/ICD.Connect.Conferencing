using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Conference
{
	public sealed class WebexParticipantInfo
	{
		#region Xml Constants

		private const string ELEMENT_CALL_ID = "CallId";
		private const string ELEMENT_INDEX = "Index";
		private const string ELEMENT_PARTICIPANT_ID = "ParticipantId";
		private const string ELEMENT_EMAIL = "Email";
		private const string ELEMENT_SPARK_USER_ID = "SparkUserId";
		private const string ELEMENT_URI = "Uri";
		private const string ELEMENT_DISPLAY_NAME = "DisplayName";
		private const string ELEMENT_STATUS = "Status";
		private const string ELEMENT_TYPE = "Type";
		private const string ELEMENT_AUDIO_MUTE = "AudioMute";
		private const string ELEMENT_OBSERVES_PARTICIPANT_ID = "ObservesParticipantId";
		private const string ELEMENT_ORG_ID = "OrgId";
		private const string ELEMENT_HARD_MUTED = "HardMuted";
		private const string ELEMENT_IS_HOST = "IsHost";
		private const string ELEMENT_CO_HOST = "CoHost";
		private const string ELEMENT_IS_PRESENTER = "IsPresenter";
		private const string ELEMENT_HAND_RAISED = "HandRaised";
		private const string ELEMENT_IS_PAIRED_TO_HOST_USER = "IsPairedToHostUser";

		#endregion

		#region Properties

		public bool IsSelf { get; set; }

		public int CallId { get; private set; }

		public int Index { get; private set; }

		public string ParticipantId { get; private set; }

		public string Email { get; private set; }

		public string SparkUserId { get; private set; }

		public string Uri { get; private set; }

		public string DisplayName { get; private set; }

		public eParticipantStatus Status { get; private set; }

		public string Type { get; private set; }

		public bool AudioMute { get; private set; }

		public string ObservesParticipantId { get; private set; }

		public string OrgId { get; private set; }

		public bool HardMuted { get; private set; }

		public bool IsHost { get; private set; }

		public bool CoHost { get; private set; }

		public bool IsPresenter { get; private set; }

		public bool HandRaised { get; private set; }

		public bool IsPairedToHostUser { get; private set; }

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callId"></param>
		/// <param name="index"></param>
		/// <param name="participantId"></param>
		/// <param name="email"></param>
		/// <param name="sparkUserId"></param>
		/// <param name="uri"></param>
		/// <param name="displayName"></param>
		/// <param name="status"></param>
		/// <param name="type"></param>
		/// <param name="audioMute"></param>
		/// <param name="observesParticipantId"></param>
		/// <param name="orgId"></param>
		/// <param name="hardMuted"></param>
		/// <param name="isHost"></param>
		/// <param name="coHost"></param>
		/// <param name="isPresenter"></param>
		/// <param name="handRaised"></param>
		/// <param name="isPairedToHostUser"></param>
		public WebexParticipantInfo(int callId, int index, string participantId, string email, string sparkUserId, string uri,
		                            string displayName, eParticipantStatus status, string type, bool audioMute,
		                            string observesParticipantId, string orgId, bool hardMuted, bool isHost, bool coHost,
		                            bool isPresenter, bool handRaised, bool isPairedToHostUser)
		{
			CallId = callId;
			Index = index;
			ParticipantId = participantId;
			Email = email;
			SparkUserId = sparkUserId;
			Uri = uri;
			DisplayName = displayName;
			Status = status;
			Type = type;
			AudioMute = audioMute;
			ObservesParticipantId = observesParticipantId;
			OrgId = orgId;
			HardMuted = hardMuted;
			IsHost = isHost;
			CoHost = coHost;
			IsPresenter = isPresenter;
			HandRaised = handRaised;
			IsPairedToHostUser = isPairedToHostUser;
		}

		public static WebexParticipantInfo FromXml(string xml)
		{
			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.ReadToNextElement();

				int callId = 0;
				int index = 0;
				string participantId = null;
				string email = null;
				string sparkUserId = null;
				string uri = null;
				string displayName = null;
				eParticipantStatus status = eParticipantStatus.Undefined;
				string type = null;
				bool audioMute = false;
				string observesParticipantId = null;
				string orgId = null;
				bool hardMuted = false;
				bool isHost = false;
				bool coHost = false;
				bool isPresenter = false;
				bool handRaised = false;
				bool isPairedToHostUser = false;

				foreach (IcdXmlReader child in reader.GetChildElements())
				{
					switch (child.Name)
					{
						case ELEMENT_CALL_ID:
							callId = child.ReadElementContentAsInt();
							break;
						case ELEMENT_INDEX:
							index = child.ReadElementContentAsInt();
							break;
						case ELEMENT_PARTICIPANT_ID:
							participantId = child.ReadElementContentAsString();
							break;
						case ELEMENT_EMAIL:
							email = child.ReadElementContentAsString();
							break;
						case ELEMENT_SPARK_USER_ID:
							sparkUserId = child.ReadElementContentAsString();
							break;
						case ELEMENT_URI:
							uri = child.ReadElementContentAsString();
							break;
						case ELEMENT_DISPLAY_NAME:
							displayName = child.ReadElementContentAsString();
							break;
						case ELEMENT_STATUS:
							status = EnumUtils.Parse<eParticipantStatus>(child.ReadElementContentAsString(), true);
							break;
						case ELEMENT_TYPE:
							type = child.ReadElementContentAsString();
							break;
						case ELEMENT_AUDIO_MUTE:
							audioMute = child.ReadElementContentAsString() == "On";
							break;
						case ELEMENT_OBSERVES_PARTICIPANT_ID:
							observesParticipantId = child.ReadElementContentAsString();
							break;
						case ELEMENT_ORG_ID:
							orgId = child.ReadElementContentAsString();
							break;
						case ELEMENT_HARD_MUTED:
							hardMuted = child.ReadElementContentAsBoolean();
							break;
						case ELEMENT_IS_HOST:
							isHost = child.ReadElementContentAsBoolean();
							break;
						case ELEMENT_CO_HOST:
							coHost = child.ReadElementContentAsBoolean();
							break;
						case ELEMENT_IS_PRESENTER:
							isPresenter = child.ReadElementContentAsBoolean();
							break;
						case ELEMENT_HAND_RAISED:
							handRaised = child.ReadElementContentAsBoolean();
							break;
						case ELEMENT_IS_PAIRED_TO_HOST_USER:
							isPairedToHostUser = child.ReadElementContentAsBoolean();
							break;
					}

					child.Dispose();
				}

				return new WebexParticipantInfo(callId, index, participantId, email, sparkUserId, uri, displayName, status, type,
				                                audioMute, observesParticipantId, orgId, hardMuted, isHost, coHost, isPresenter,
				                                handRaised, isPairedToHostUser);
			}
		}

		#endregion
	}
}