using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
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