using ICD.Connect.Conferencing.Zoom.Components.Bookings;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("BookingsListResult", eZoomRoomApiType.zEvent, true)]
	[JsonConverter(typeof(BookingsListCommandResponseConverter))]
	public sealed class BookingsListCommandResponse : AbstractZoomRoomResponse
	{
		public Booking[] Bookings { get; set; }
	}
}