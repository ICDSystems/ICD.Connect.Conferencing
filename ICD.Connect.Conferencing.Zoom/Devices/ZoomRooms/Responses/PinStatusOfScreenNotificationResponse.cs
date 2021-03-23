using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("PinStatusOfScreenNotification", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(PinStatusOfScreenNotificationResponseConverter))]
	public sealed class PinStatusOfScreenNotificationResponse : AbstractZoomRoomResponse
	{
		public PinStatusOfScreenNotification PinStatusOfScreenNotification { get; set; }
	}
}
