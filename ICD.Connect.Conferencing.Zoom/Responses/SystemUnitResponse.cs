using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("SystemUnit", eZoomRoomApiType.zStatus, true)]
	public sealed class SystemUnitResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("SystemUnit")]
		public SystemInfo SystemInfo { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			SystemInfo = new SystemInfo();
			SystemInfo.LoadFromJObject((JObject) jObject["SystemUnit"]);
		}
	}

	public sealed class SystemInfo : AbstractZoomRoomData
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

		public override void LoadFromJObject(JObject jObject)
		{
			Email = jObject["email"].ToString();
			LoginType = jObject["login_type"].ToObject<eLoginType>();
			MeetingNumber = jObject["meeting_number"].ToString();
			Platform = jObject["platform"].ToString();
			RoomVersion = jObject["room_version"].ToString();

			RoomInfo = new RoomInfo();
			RoomInfo.LoadFromJObject((JObject) jObject["room_info"]);
		}
	}

	public enum eLoginType
	{
		google,
		work_email
	}

	public sealed class RoomInfo : AbstractZoomRoomData
	{
		[JsonProperty("room_name")]
		public string RoomName { get; set; }

		[JsonProperty("is_auto_answer_enabled")]
		public bool IsAutoAnswerEnabled { get; set; }

		[JsonProperty("is_auto_answer_selected")]
		public bool IsAutoAnswerSelected { get; set; }

		[JsonProperty("account_email")]
		public string AccountEmail { get; set; }

		public override void LoadFromJObject(JObject jObject)
		{
			RoomName = jObject["room_name"].ToString();
			IsAutoAnswerEnabled = jObject["is_auto_answer_enabled"].ToObject<bool>();
			IsAutoAnswerSelected = jObject["is_auto_answer_selected"].ToObject<bool>();
			AccountEmail = jObject["account_email"].ToString();
		}
	}
}