using System;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.Zoom.Components.Bookings;

namespace ICD.Connect.Conferencing.Zoom.Controls.Calendar
{
    public sealed class ZoomBooking : AbstractBooking, IZoomBooking
    {
	    private readonly Booking m_Booking;

	    public override string MeetingName
	    {
		    get { return m_Booking.MeetingName; }
		}

	    public string MeetingNumber
	    {
		    get { return m_Booking.MeetingNumber; }
	    }

	    public string SipUri
	    {
		    get { return "sip:" + m_Booking.MeetingNumber + "@zmus.us"; }
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

		public override eMeetingType Type
	    {
		    get { return eMeetingType.VideoConference; }
	    }

	    public ZoomBooking(Booking booking)
	    {
		    m_Booking = booking;
	    }
    }
}
