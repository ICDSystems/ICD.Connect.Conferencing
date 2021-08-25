#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Call
{
	[JsonConverter(typeof(CallInfoConverter))]
	public sealed class CallInfo
	{
		//public CallInOutLists CallInOutInfo { get; set; }

		public string RealMeetingId { get; set; }

		public string MeetingId { get; set; }

		public string ParticipantId { get; set; }

		public string MyUserId { get; set; }

		public bool AmIOriginalHost { get; set; }

		public bool IsWebinar { get; set; }

		public bool IsViewOnly { get; set; }

		public eMeetingType MeetingType { get; set; }

		public string MeetingPassword { get; set; }

		public string DialIn { get; set; }

		//public string TollFreeNumber { get; set; }

		//public string InternationalUrl { get; set; }

		//public eCalloutType SupportCalloutType { get; set; }

		//public eUserType UserType { get; set; }

		//public string InviteEmailSubject { get; set; }

		//public string InviteEmailContent { get; set; }
	}
}