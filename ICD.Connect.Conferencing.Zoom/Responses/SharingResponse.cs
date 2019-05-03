using ICD.Connect.Conferencing.Zoom.Components.Presentation;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Sharing", eZoomRoomApiType.zStatus, true), 
	 ZoomRoomApiResponse("Sharing", eZoomRoomApiType.zStatus, false)]
	[JsonConverter(typeof(SharingResponseConverter))]
	public sealed class SharingResponse : AbstractZoomRoomResponse
	{
		public SharingInfo Sharing { get; set; }
	}
}