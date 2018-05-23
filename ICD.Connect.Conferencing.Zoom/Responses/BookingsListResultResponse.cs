using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Connect.Conferencing.Contacts;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("BookingsListResult", eZoomRoomApiType.zCommand, true)]
	public sealed class BookingsListCommandResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("BookingsListResult")]
		public ZoomBooking[] Bookings { get; private set; }
	}

	public sealed class ZoomBooking : IContact
	{
		[JsonProperty("meetingName")]
		public string Name { get; private set; }

		[JsonProperty("meetingNumber")]
		public string MeetingNumber { get; private set; }

		[JsonProperty("hostName")]
		public string HostName { get; private set; }

		[JsonProperty("startTime")]
		public DateTime StartTime { get; private set; }

		[JsonProperty("endTime")]
		public DateTime EndTime { get; private set; }

		[JsonProperty("creatorName")]
		public string CreatorName { get; private set; }

		[JsonProperty("creatorEmail")]
		public string CreatorEmail { get; private set; }

		public IEnumerable<IContactMethod> GetContactMethods()
		{
			yield return new ContactMethod(MeetingNumber);
		}
	}
}