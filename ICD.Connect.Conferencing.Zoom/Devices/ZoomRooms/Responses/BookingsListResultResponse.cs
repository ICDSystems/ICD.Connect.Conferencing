using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Bookings;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("BookingsListResult", eZoomRoomApiType.zEvent, true)]
	[JsonConverter(typeof(BookingsListCommandResponseConverter))]
	public sealed class BookingsListCommandResponse : AbstractZoomRoomResponse
	{
		public Booking[] Bookings { get; set; }
	}
}