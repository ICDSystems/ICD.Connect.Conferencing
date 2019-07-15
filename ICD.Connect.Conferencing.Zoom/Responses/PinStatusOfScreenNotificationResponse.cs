using ICD.Connect.Conferencing.Zoom.Components.Presentation;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("PinStatusOfScreenNotification", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(PinStatusOfScreenNotificationResponseConverter))]
	public sealed class PinStatusOfScreenNotificationResponse : AbstractZoomRoomResponse
	{
		public PinStatusOfScreenNotification PinStatusOfScreenNotification { get; set; }
	}
}
