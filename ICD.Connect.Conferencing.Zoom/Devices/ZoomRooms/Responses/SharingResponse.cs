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
	[ZoomRoomApiResponse("Sharing", eZoomRoomApiType.zStatus, true), 
	 ZoomRoomApiResponse("Sharing", eZoomRoomApiType.zStatus, false)]
	[JsonConverter(typeof(SharingResponseConverter))]
	public sealed class SharingResponse : AbstractZoomRoomResponse
	{
		public SharingInfo Sharing { get; set; }
	}
}