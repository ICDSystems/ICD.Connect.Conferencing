using System;
using System.Linq;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Calendar;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls.Calender
{
    public sealed class PolycomBooking : AbstractBooking, ISipBooking
    {
	    private readonly MeetingInfo m_Booking;

	    public override string MeetingName
	    {
		    get { return m_Booking.Subject; }
		}

		public string SipUri
		{
			get
			{
				return m_Booking.GetDialingNumbers()
					.Where(n => n.Protocol.Equals("sip", StringComparison.OrdinalIgnoreCase))
					.Select(n => n.Number)
					.FirstOrDefault();
			}
		}

		public override string OrganizerName
	    {
		    get { return m_Booking.Organizer;  }
	    }

	    public override string OrganizerEmail
	    {
	        get { return null; }
	    }

	    public override DateTime StartTime
	    {
			get { return m_Booking.Start; }
		}

	    public override DateTime EndTime
	    {
			get { return m_Booking.End; }
		}

	    public override bool IsPrivate
	    {
		    get { return !m_Booking.Public; }
	    }

		public override eMeetingType Type
	    {
		    get { return eMeetingType.VideoConference; }
	    }

	    public PolycomBooking(MeetingInfo booking)
	    {
		    m_Booking = booking;
	    }
    }
}
