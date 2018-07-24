using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Models;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("IncomingCallIndication", eZoomRoomApiType.zEvent, false)]
	public sealed class IncomingCallResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("IncomingCallIndication")]
		public IncomingCall IncomingCall { get; private set; }
	}
}