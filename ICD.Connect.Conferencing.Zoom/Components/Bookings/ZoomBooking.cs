using System;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Bookings
{
	public sealed class ZoomBooking
	{
		/// <summary>
		/// Name of the meeting
		/// </summary>
		[JsonProperty("meetingName")]
		public string MeetingName { get; private set; }

		/// <summary>
		/// Scheduled start time of the meeting
		/// </summary>
		[JsonProperty("startTime")]
		public DateTime StartTime { get; private set; }

		/// <summary>
		/// Scheduled end time for the meeting
		/// </summary>
		[JsonProperty("endTime")]
		public DateTime EndTime { get; private set; }

		/// <summary>
		/// Name of the person who created the meeting.
		/// </summary>
		/// <remarks>
		/// From Zoom API docs: Typically empty.
		/// </remarks>
		[JsonProperty("creatorName")]
		public string OrganizerName { get; private set; }

		/// <summary>
		/// Email of the person who created the meeting
		/// </summary>
		[JsonProperty("creatorEmail")]
		public string OrganizerEmail { get; private set; }

		/// <summary>
		/// Zoom meeting id to join for the meeting
		/// </summary>
		[JsonProperty("meetingNumber")]
		public string MeetingNumber { get; private set; }

		/// <summary>
		/// Whether the meeting private or not.
		/// </summary>
		[JsonProperty("isPrivate")]
		public bool IsPrivate { get; private set; }

		/// <summary>
		/// Name of the host for the meeting.
		/// </summary>
		/// <remarks>
		/// From Zoom API docs: Typically empty, reserved for future use.
		/// </remarks>
		[JsonProperty("hostName")]
		public string HostName { get; private set; }
	}
}