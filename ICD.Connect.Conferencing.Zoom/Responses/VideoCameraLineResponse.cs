using ICD.Connect.Conferencing.Zoom.Components.Camera;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Video Camera Line", eZoomRoomApiType.zStatus, true),
	 ZoomRoomApiResponse("Video Camera Line", eZoomRoomApiType.zStatus, false)]
	public class VideoCameraLineResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Video Camera Line")]
		public CameraInfo[] Cameras { get; private set; }
	}
}