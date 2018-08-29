using System.Collections.Generic;
using ICD.Connect.Conferencing.Zoom.Controls.Calendar;

namespace ICD.Connect.Conferencing.Zoom.Comparers
{
	public sealed class BookingsComparer : IEqualityComparer<ZoomBooking>
	{
		public bool Equals(ZoomBooking x, ZoomBooking y)
		{
			return x.MeetingNumber == y.MeetingNumber
			       && x.MeetingName == y.MeetingName
				   && x.OrganizerName == y.OrganizerName
				   && x.OrganizerEmail == y.OrganizerEmail
				   && x.StartTime == y.StartTime
				   && x.EndTime == y.EndTime;
		}

		public int GetHashCode(ZoomBooking zoomBooking)
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + (zoomBooking.MeetingName == null ? 0 : zoomBooking.MeetingName.GetHashCode());
				hash = hash * 23 + (zoomBooking.OrganizerEmail == null ? 0 : zoomBooking.OrganizerEmail.GetHashCode());
				hash = hash * 23 + (zoomBooking.OrganizerName == null ? 0 : zoomBooking.OrganizerName.GetHashCode());
				hash = hash * 23 + (zoomBooking.MeetingNumber == null ? 0 : zoomBooking.MeetingNumber.GetHashCode());
				hash = hash * 23 + (int)zoomBooking.StartTime.Ticks;
				hash = hash * 23 + (int)zoomBooking.EndTime.Ticks;
				return hash;
			}
		}
	}
}
