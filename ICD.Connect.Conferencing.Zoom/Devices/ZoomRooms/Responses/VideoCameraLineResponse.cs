#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Camera;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("Video Camera Line", eZoomRoomApiType.zStatus, true),
	 ZoomRoomApiResponse("Video Camera Line", eZoomRoomApiType.zStatus, false)]
	[JsonConverter(typeof(VideoCameraLineResponseConverter))]
	public sealed class VideoCameraLineResponse : AbstractZoomRoomResponse
	{
		public CameraInfo[] Cameras { get; set; }
	}
}