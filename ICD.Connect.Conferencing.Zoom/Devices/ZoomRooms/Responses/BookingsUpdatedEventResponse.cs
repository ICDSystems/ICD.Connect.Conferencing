using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("Bookings Updated", eZoomRoomApiType.zEvent, false),
	 ZoomRoomApiResponse("Bookings Updated", eZoomRoomApiType.zEvent, true)]
	[JsonConverter(typeof(BookingsUpdatedEventResponseConverter))]
	public sealed class BookingsUpdatedEventResponse : AbstractZoomRoomResponse
	{
	}
}
