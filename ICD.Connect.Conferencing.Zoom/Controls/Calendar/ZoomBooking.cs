using System;
using System.Collections.Generic;
using ICD.Connect.Calendaring.Bookings;
using ICD.Connect.Conferencing.DialContexts;
using Booking = ICD.Connect.Conferencing.Zoom.Components.Bookings.Booking;

namespace ICD.Connect.Conferencing.Zoom.Controls.Calendar
{
    public sealed class ZoomBooking : AbstractBooking
    {
	    private readonly Booking m_Booking;

	    public override string MeetingName
	    {
		    get { return m_Booking.MeetingName; }
		}

		public override string OrganizerName
	    {
		    get { return m_Booking.OrganizerName;  }
	    }

	    public override string OrganizerEmail
	    {
			get { return m_Booking.OrganizerEmail; }
		}

	    public override DateTime StartTime
	    {
			get { return m_Booking.StartTime; }
		}

	    public override DateTime EndTime
	    {
			get { return m_Booking.EndTime; }
		}

	    public override bool IsPrivate
	    {
		    get { return m_Booking.IsPrivate; }
	    }

	    public override bool CheckedIn
	    {
		    get { return m_Booking.CheckedIn; }
	    }

	    public override bool CheckedOut
	    {
		    get { return false; }
	    }

	    public override IEnumerable<IDialContext> GetBookingNumbers()
	    {
		    if (!string.IsNullOrEmpty(m_Booking.MeetingNumber))
			    yield return new DialContext {Protocol = eDialProtocol.Zoom, DialString = m_Booking.MeetingNumber};
	    }

	    public ZoomBooking(Booking booking)
	    {
		    m_Booking = booking;
	    }
    }
}
