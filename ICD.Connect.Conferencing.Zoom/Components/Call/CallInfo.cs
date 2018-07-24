using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	public sealed class CallInfo
	{
		[JsonProperty("Info")]
		public CallInOutLists CallInOutInfo { get; private set; }

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

		[JsonProperty("toll_free_number")]
		public string TollFreeNumber { get; private set; }

		[JsonProperty("international_url")]
		public string InternationalUrl { get; private set; }

		[JsonProperty("support_callout_type")]
		public eCalloutType SupportCalloutType { get; private set; }

		[JsonProperty("user_type")]
		public eUserType UserType { get; private set; }

		[JsonProperty("invite_email_subject")]
		public string InviteEmailSubject { get; private set; }

		[JsonProperty("invite_email_content")]
		public string InviteEmailContent { get; private set; }
	}
}