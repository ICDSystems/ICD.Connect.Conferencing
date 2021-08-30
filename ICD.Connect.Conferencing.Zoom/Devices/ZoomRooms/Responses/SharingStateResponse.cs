#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("SharingState", eZoomRoomApiType.zEvent, true),
	 ZoomRoomApiResponse("SharingState", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(SharingStateResponseConverter))]
	public sealed class SharingStateResponse : AbstractZoomRoomResponse
	{
		public SharingStateInfo SharingState { get; set; }
	}
}