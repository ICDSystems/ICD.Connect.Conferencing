using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("CallConnectError", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(CallConnectErrorResponseConverter))]
	public sealed class CallConnectErrorResponse : AbstractZoomRoomResponse
	{
		public CallConnectError Error { get; set; }
	}

	[JsonConverter(typeof(CallConnectErrorConverter))]
	public sealed class CallConnectError
	{
		public int ErrorCode { get; set; }

		public string ErrorMessage { get; set; }
	}
}