using ICD.Connect.Conferencing.Zoom.Responses;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	public sealed class CallInfo : AbstractZoomRoomData
	{
		//[JsonProperty("Info")]
		//public CallInOutLists CallInOutInfo { get; private set; }

		[JsonProperty("real_meeting_id")]
		public string RealMeetingId { get; private set; }

		[JsonProperty("meeting_id")]
		public string MeetingId { get; private set; }

		[JsonProperty("participant_id")]
		public string ParticipantId { get; private set; }

		[JsonProperty("my_userid")]
		public string MyUserId { get; private set; }

		[JsonProperty("am_i_original_host")]
		public bool AmIOriginalHost { get; private set; }

		[JsonProperty("is_webinar")]
		public bool IsWebinar { get; private set; }

		[JsonProperty("is_view_only")]
		public bool IsViewOnly { get; private set; }

		[JsonProperty("meeting_type")]
		public eMeetingType MeetingType { get; private set; }

		[JsonProperty("meeting_password")]
		public string MeetingPassword { get; private set; }

		[JsonProperty("dialIn")]
		public string DialIn { get; private set; }

		//[JsonProperty("toll_free_number")]
		//public string TollFreeNumber { get; private set; }

		//[JsonProperty("international_url")]
		//public string InternationalUrl { get; private set; }

		//[JsonProperty("support_callout_type")]
		//public eCalloutType SupportCalloutType { get; private set; }

		//[JsonProperty("user_type")]
		//public eUserType UserType { get; private set; }

		//[JsonProperty("invite_email_subject")]
		//public string InviteEmailSubject { get; private set; }

		//[JsonProperty("invite_email_content")]
		//public string InviteEmailContent { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			RealMeetingId = jObject["real_meeting_id"].ToString();
			MeetingId = jObject["meeting_id"].ToString();
			ParticipantId = jObject["participant_id"].ToString();
			MyUserId = jObject["my_userid"].ToString();
			AmIOriginalHost = jObject["am_i_original_host"].ToObject<bool>();
			IsWebinar = jObject["is_webinar"].ToObject<bool>();
			IsViewOnly = jObject["is_view_only"].ToObject<bool>();
			MeetingType = jObject["meeting_type"].ToObject<eMeetingType>();
			MeetingPassword = jObject["meeting_password"].ToString();
			DialIn = jObject["dialIn"].ToString();
		}
	}
}