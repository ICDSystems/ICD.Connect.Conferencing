using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

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