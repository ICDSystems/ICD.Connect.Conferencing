using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Call", eZoomRoomApiType.zStatus, true),
	 ZoomRoomApiResponse("Call", eZoomRoomApiType.zStatus, false)]
	public sealed class CallStatusResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Call")]
		public CallStatusInfo CallStatus { get; private set; }
	}

	public sealed class CallStatusInfo
	{
		[JsonProperty("Status")]
		public eCallStatus Status { get; private set; }
	}
	
	public enum eCallStatus
	{
		NOT_IN_MEETING,
		CONNECTING_MEETING,
		IN_MEETING,
		LOGGED_OUT,
		UNKNOWN
	}
}