#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	public enum eLoginType
	{
		google,
		work_email
	}

	[ZoomRoomApiResponse("SystemUnit", eZoomRoomApiType.zStatus, true)]
	[JsonConverter(typeof(SystemUnitResponseConverter))]
	public sealed class SystemUnitResponse : AbstractZoomRoomResponse
	{
		public SystemInfo SystemInfo { get; set; }
	}

	[JsonConverter(typeof(SystemInfoConverter))]
	public sealed class SystemInfo
	{
		public string Email { get; set; }

		public eLoginType LoginType { get; set; }

		public string MeetingNumber { get; set; }

		/// <summary>
		/// Platform the Zoom Room software is running on
		/// </summary>
		public string Platform { get; set; }

		public RoomInfo RoomInfo { get; set; }

		public string RoomVersion { get; set; }
	}

	[JsonConverter(typeof(RoomInfoConverter))]
	public sealed class RoomInfo
	{
		public string RoomName { get; set; }

		public bool IsAutoAnswerEnabled { get; set; }

		public bool IsAutoAnswerSelected { get; set; }

		public string AccountEmail { get; set; }
	}
}