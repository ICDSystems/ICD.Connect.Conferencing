using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Calendaring.Bookings;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using Booking = ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings.Booking;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Calender
{
    public sealed class CiscoBooking : AbstractBooking
    {
	    private readonly Booking m_Booking;
	    private readonly List<IDialContext> m_BookingNumbers;

	    public override string MeetingName
	    {
		    get { return m_Booking.Title; }
		}

        public override string OrganizerName
        {
            get { return string.Format("{0} {1}", m_Booking.OrganizerFirstName, m_Booking.OrganizerLastName).Trim(); }
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
		    get { return m_Booking.Privacy == Booking.ePrivacy.Private; }
	    }

	    public override bool CheckedIn { get { return false; } }
	    public override bool CheckedOut { get { return false; } }

	    public override IEnumerable<IDialContext> GetBookingNumbers()
	    {
			return m_BookingNumbers.ToArray(m_BookingNumbers.Count);
		}

	    public CiscoBooking(Booking booking)
	    {
		    m_Booking = booking;
		    m_BookingNumbers = ParseBookingNumbers().ToList();
	    }

	    private IEnumerable<IDialContext> ParseBookingNumbers()
	    {
		    foreach (BookingCall call in m_Booking.GetCalls())
		    {
				switch (call.Protocol.ToUpper())
				{
					case "SIP":
						yield return new DialContext
						{
							Protocol = eDialProtocol.Sip,
							DialString = call.Number,
							CallType = call.CiscoCallType.ToCallType()
						};
						continue;
					case "SPARK":
						yield return new DialContext
						{
							Protocol = eDialProtocol.Sip,
							DialString = call.Number,
							//Spark calls are all video, even though they don't tell us that
							CallType = eCallType.Video
						};
						continue;
			    }
			}
	    }
    }
}
