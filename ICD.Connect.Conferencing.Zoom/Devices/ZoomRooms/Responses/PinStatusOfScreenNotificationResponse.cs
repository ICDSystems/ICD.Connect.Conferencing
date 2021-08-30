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
	[ZoomRoomApiResponse("PinStatusOfScreenNotification", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(PinStatusOfScreenNotificationResponseConverter))]
	public sealed class PinStatusOfScreenNotificationResponse : AbstractZoomRoomResponse
	{
		public PinStatusOfScreenNotification PinStatusOfScreenNotification { get; set; }
	}
}
