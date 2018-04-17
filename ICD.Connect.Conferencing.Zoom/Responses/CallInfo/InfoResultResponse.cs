using System.Collections.Generic;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.CallInfo
{
	/// <summary>
	/// Contains call info, received either as an event, status update, or command
	/// </summary>
	[ZoomRoomApiResponse("InfoResult")]
	public class InfoResultResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("InfoResult")]
		public CallInfo InfoResult { get; set; }
	}

	public class CallInfo
	{
		[JsonProperty("Info")]
		public CallInOutLists Info { get; set; }

		[JsonProperty("real_meeting_id")]
		public string RealMeetingId { get; set; }

		[JsonProperty("meeting_id")]
		public string MeetingId { get; set; }

		[JsonProperty("participant_id")]
		public string ParticipantId { get; set; }

		[JsonProperty("my_userid")]
		public string MyUserId { get; set; }

		[JsonProperty("am_i_original_host")]
		public bool AmIOriginalHost { get; set; }

		[JsonProperty("is_webinar")]
		public bool IsWebinar { get; set; }

		[JsonProperty("is_view_only")]
		public bool IsViewOnly { get; set; }

		[JsonProperty("meeting_type")]
		public eMeetingType MeetingType { get; set; }

		[JsonProperty("meeting_password")]
		public string MeetingPassword { get; set; }

		[JsonProperty("dialIn")]
		public string DialIn { get; set; }

		[JsonProperty("toll_free_number")]
		public string TollFreeNumber { get; set; }

		[JsonProperty("international_url")]
		public string InternationalUrl { get; set; }

		[JsonProperty("support_callout_type")]
		public eCalloutType SupportCalloutType { get; set; }

		[JsonProperty("user_type")]
		public eUserType UserType { get; set; }

		[JsonProperty("invite_email_subject")]
		public string InviteEmailSubject { get; set; }

		[JsonProperty("invite_email_content")]
		public string InviteEmailContent { get; set; }
	}

	public class CallInOutLists
	{
		[JsonProperty("callout_country_list")]
		public List<CallInOutListEntry> CalloutCountryList { get; set; }

		[JsonProperty("callin_country_list")]
		public List<CallInOutListEntry> CallinCountryList { get; set; }

		[JsonProperty("toll_free_callin_list")]
		public List<CallInOutListEntry> TollFreeCallinList { get; set; }
	}

	public class CallInOutListEntry
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("name")]
		public string Name { get; set; }

		[JsonProperty("code")]
		public string Code { get; set; }

		[JsonProperty("number")]
		public string Number { get; set; }

		[JsonProperty("display_number")]
		public string DisplayNumber { get; set; }
	}

	public enum eMeetingType
	{
		NONE,
		NORMAL,
		SHARING_LAPTOP,
		PSTN_CALLOUT
	}

	public enum eCalloutType
	{
		NONE,
		US_ONLY,
		INTERNATIONAL
	}

	public enum eUserType
	{
		NONE,
		BASIC,
		PRO,
		CORP
	}
}