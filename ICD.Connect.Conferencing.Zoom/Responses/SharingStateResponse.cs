using ICD.Connect.Conferencing.Zoom.Components.Presentation;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("SharingState", eZoomRoomApiType.zEvent, true),
	 ZoomRoomApiResponse("SharingState", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(SharingStateResponseConverter))]
	public sealed class SharingStateResponse : AbstractZoomRoomResponse
	{
		public SharingStateInfo SharingState { get; set; }
	}
}