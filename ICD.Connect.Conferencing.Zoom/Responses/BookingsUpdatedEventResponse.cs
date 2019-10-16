using ICD.Connect.Conferencing.Zoom.Responses.Attributes;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Bookings Updated", eZoomRoomApiType.zEvent, false),
	 ZoomRoomApiResponse("Bookings Updated", eZoomRoomApiType.zEvent, true)]
	public sealed class BookingsUpdatedEventResponse : AbstractZoomRoomResponse
	{
		
	}
}
