using ICD.Connect.Conferencing.Zoom.Components.Bookings;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("BookingsListResult", eZoomRoomApiType.zEvent, true)]
	public sealed class BookingsListCommandResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("BookingsListResult")]
		public Booking[] Bookings { get; private set; }
	}
}