using ICD.Connect.Conferencing.Zoom.Models;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("BookingsListResult", eZoomRoomApiType.zCommand, true)]
	public sealed class BookingsListCommandResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("BookingsListResult")]
		public ZoomBooking[] Bookings { get; private set; }
	}
}