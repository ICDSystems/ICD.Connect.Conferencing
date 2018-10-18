﻿using System;
using System.Collections.Generic;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.Zoom.Components.Bookings;

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

	    public override IEnumerable<IBookingNumber> GetBookingNumbers()
	    {
			yield return new ZoomBookingNumber(m_Booking.MeetingNumber);
		}

		public ZoomBooking(Booking booking)
	    {
		    m_Booking = booking;
	    }
    }
}
