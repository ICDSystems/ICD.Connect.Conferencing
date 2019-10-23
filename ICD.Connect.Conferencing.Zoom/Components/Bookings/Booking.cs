using System;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Bookings
{
	[JsonConverter(typeof(BookingConverter))]
	public sealed class Booking : IEquatable<Booking>
	{
		#region Properties

		/// <summary>
		/// Name of the meeting
		/// </summary>
		public string MeetingName { get; set; }

		/// <summary>
		/// Scheduled start time of the meeting
		/// </summary>
		public DateTime StartTime { get; set; }

		/// <summary>
		/// Scheduled end time for the meeting
		/// </summary>
		public DateTime EndTime { get; set; }

		/// <summary>
		/// Name of the person who created the meeting.
		/// </summary>
		/// <remarks>
		/// From Zoom API docs: Typically empty.
		/// </remarks>
		public string OrganizerName { get; set; }

		/// <summary>
		/// Email of the person who created the meeting
		/// </summary>
		public string OrganizerEmail { get; set; }

		/// <summary>
		/// Zoom meeting id to join for the meeting
		/// </summary>
		public string MeetingNumber { get; set; }

		/// <summary>
		/// Whether the meeting private or not.
		/// </summary>
		public bool IsPrivate { get; set; }

		/// <summary>
		/// Name of the host for the meeting.
		/// </summary>
		/// <remarks>
		/// From Zoom API docs: Typically empty, reserved for future use.
		/// </remarks>
		public string HostName { get; set; }

		/// <summary>
		/// Whether the meeting has been checked into or not by the ZoomRoom.
		/// </summary>
		public bool CheckIn { get; set; }

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