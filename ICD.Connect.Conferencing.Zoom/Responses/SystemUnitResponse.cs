using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("SystemUnit", eZoomRoomApiType.zStatus, true)]
	public sealed class SystemUnitResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("SystemUnit")]
		public SystemInfo SystemInfo { get; private set; }
	}

	public sealed class SystemInfo
	{
		[JsonProperty("email")]
		public string Email { get; set; }

		[JsonProperty("login_type")]
		public eLoginType LoginType { get; set; }

		[JsonProperty("meeting_number")]
		public string MeetingNumber { get; set; }

		/// <summary>
		/// Platform the Zoom Room software is running on
		/// </summary>
		[JsonProperty("platform")]
		public string Platform { get; set; }

		[JsonProperty("room_info")]
		public RoomInfo RoomInfo { get; set; }

		[JsonProperty("room_version")]
		public string RoomVersion { get; set; }
	}

	public enum eLoginType
	{
		google,
		work_email
	}

	public sealed class RoomInfo
	{
		[JsonProperty("room_name")]
		public string RoomName { get; set; }

		[JsonProperty("is_auto_answer_enabled")]
		public bool IsAutoAnswerEnabled { get; set; }

		[JsonProperty("is_auto_answer_selected")]
		public bool IsAutoAnswerSelected { get; set; }

		[JsonProperty("account_email")]
		public string AccountEmail { get; set; }
	}
}