using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	public enum eCallStatus
	{
		UNKNOWN,
		NOT_IN_MEETING,
		CONNECTING_MEETING,
		IN_MEETING,
		LOGGED_OUT
	}

	[ZoomRoomApiResponse("Call", eZoomRoomApiType.zStatus, true),
	 ZoomRoomApiResponse("Call", eZoomRoomApiType.zStatus, false)]
	[JsonConverter(typeof(CallStatusResponseConverter))]
	public sealed class CallStatusResponse : AbstractZoomRoomResponse
	{
		public CallStatusInfo CallStatus { get; set; }
	}

	[JsonConverter(typeof(CallStatusInfoConverter))]
	public sealed class CallStatusInfo
	{
		public eCallStatus? Status { get; set; }

		public ClosedCaption ClosedCaption { get; set; }
	}

	[JsonConverter(typeof(ClosedCaptionConverter))]
	public sealed class ClosedCaption
	{
		public bool Available { get; set; }
	}
}