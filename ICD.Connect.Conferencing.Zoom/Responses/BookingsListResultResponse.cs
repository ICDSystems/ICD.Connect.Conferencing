using System.Linq;
using ICD.Connect.Conferencing.Zoom.Components.Bookings;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("BookingsListResult", eZoomRoomApiType.zEvent, true)]
	public sealed class BookingsListCommandResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("BookingsListResult")]
		public Booking[] Bookings { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			Bookings = jObject["BookingsListResult"].Children().Select(o =>
			{
				var booking = new Booking();
				booking.LoadFromJObject((JObject) o);
				return booking;
			}).ToArray();
		}
	}
}