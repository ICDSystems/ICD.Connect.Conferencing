#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Call;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("IncomingCallIndication", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(IncomingCallResponseConverter))]
	public sealed class IncomingCallResponse : AbstractZoomRoomResponse
	{
		public IncomingCall IncomingCall { get; set; }
	}
}