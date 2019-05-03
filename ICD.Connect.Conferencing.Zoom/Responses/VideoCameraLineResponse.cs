using ICD.Connect.Conferencing.Zoom.Components.Camera;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Video Camera Line", eZoomRoomApiType.zStatus, true),
	 ZoomRoomApiResponse("Video Camera Line", eZoomRoomApiType.zStatus, false)]
	[JsonConverter(typeof(VideoCameraLineResponseConverter))]
	public sealed class VideoCameraLineResponse : AbstractZoomRoomResponse
	{
		public CameraInfo[] Cameras { get; set; }
	}
}