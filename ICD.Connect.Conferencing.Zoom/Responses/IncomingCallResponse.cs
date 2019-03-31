using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("IncomingCallIndication", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(IncomingCallResponseConverter))]
	public sealed class IncomingCallResponse : AbstractZoomRoomResponse
	{
		public IncomingCall IncomingCall { get; set; }
	}
}