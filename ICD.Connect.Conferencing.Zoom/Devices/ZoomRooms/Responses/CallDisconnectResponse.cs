using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("CallDisconnect", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(CallDisconnectResponseConverter))]
	public sealed class CallDisconnectResponse : AbstractZoomRoomResponse
	{
		public CallDisconnect Disconnect { get; set; }
	}

	[JsonConverter(typeof(CallDisconnectConverter))]
	public sealed class CallDisconnect
	{
		public eZoomBoolean Success { get; set; }
	}
}