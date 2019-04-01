using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class CallInfoConverter : AbstractGenericJsonConverter<CallInfo>
	{
		private const string ATTR_INFO = "Info";
		private const string ATTR_REAL_MEETING_ID = "real_meeting_id";
		private const string ATTR_MEETING_ID = "meeting_id";
		private const string ATTR_PARTICIPANT_ID = "participant_id";
		private const string ATTR_MY_USERID = "my_userid";
		private const string ATTR_AM_I_ORIGINAL_HOST = "am_i_original_host";
		private const string ATTR_IS_WEBINAR = "is_webinar";
		private const string ATTR_IS_VIEW_ONLY = "is_view_only";
		private const string ATTR_MEETING_TYPE = "meeting_type";
		private const string ATTR_MEETING_PASSWORD = "meeting_password";
		private const string ATTR_DIAL_IN = "dialIn";
		private const string ATTR_TOLL_FREE_NUMBER = "toll_free_number";
		private const string ATTR_INTERNATIONAL_URL = "international_url";
		private const string ATTR_SUPPORT_CALLOUT_TYPE = "support_callout_type";
		private const string ATTR_USER_TYPE = "user_type";
		private const string ATTR_INVITE_EMAIL_SUBJECT = "invite_email_subject";
		private const string ATTR_INVITE_EMAIL_CONTENT = "invite_email_content";

		protected override void ReadProperty(string property, JsonReader reader, CallInfo instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_REAL_MEETING_ID:
					instance.RealMeetingId = reader.GetValueAsString();
					break;
				case ATTR_MEETING_ID:
					instance.MeetingId = reader.GetValueAsString();
					break;
				case ATTR_PARTICIPANT_ID:
					instance.ParticipantId = reader.GetValueAsString();
					break;
				case ATTR_MY_USERID:
					instance.MyUserId = reader.GetValueAsString();
					break;
				case ATTR_AM_I_ORIGINAL_HOST:
					instance.AmIOriginalHost = reader.GetValueAsBool();
					break;
				case ATTR_IS_WEBINAR:
					instance.IsWebinar = reader.GetValueAsBool();
					break;
				case ATTR_IS_VIEW_ONLY:
					instance.IsViewOnly = reader.GetValueAsBool();
					break;
				case ATTR_MEETING_TYPE:
					instance.MeetingType = reader.GetValueAsEnum<eMeetingType>();
					break;
				case ATTR_MEETING_PASSWORD:
					instance.MeetingPassword = reader.GetValueAsString();
					break;
				case ATTR_DIAL_IN:
					instance.DialIn = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}