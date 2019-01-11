using ICD.Connect.Conferencing.Zoom.Components.Presentation;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Sharing", eZoomRoomApiType.zStatus, true), 
	 ZoomRoomApiResponse("Sharing", eZoomRoomApiType.zStatus, false)]
	public sealed class SharingResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Sharing")]
		public SharingInfo Sharing { get; set; }
	}
}