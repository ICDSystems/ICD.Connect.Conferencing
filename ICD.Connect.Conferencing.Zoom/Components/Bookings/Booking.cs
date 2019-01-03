using System;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Bookings
{
	public sealed class Booking : IEquatable<Booking>
	{
		#region Properties

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

		#endregion

		#region Methods

		public override bool Equals(object obj)
		{
			return Equals(obj as Booking);
		}

		public bool Equals(Booking other)
		{
			return other != null &&
			       other.MeetingName == MeetingName &&
			       other.StartTime == StartTime &&
			       other.EndTime == EndTime &&
			       other.OrganizerName == OrganizerName &&
			       other.OrganizerEmail == OrganizerEmail &&
			       other.MeetingNumber == MeetingNumber &&
			       other.IsPrivate == IsPrivate &&
			       other.HostName == HostName;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + (MeetingName == null ? 0 : MeetingName.GetHashCode());
				hash = hash * 23 + StartTime.GetHashCode();
				hash = hash * 23 + EndTime.GetHashCode();
				hash = hash * 23 + (OrganizerName == null ? 0 : OrganizerName.GetHashCode());
				hash = hash * 23 + (OrganizerEmail == null ? 0 : OrganizerEmail.GetHashCode());
				hash = hash * 23 + (MeetingNumber == null ? 0 : MeetingNumber.GetHashCode());
				hash = hash * 23 + IsPrivate.GetHashCode();
				hash = hash * 23 + (HostName == null ? 0 : HostName.GetHashCode());
				return hash;
			}
		}

		#endregion
	}
}