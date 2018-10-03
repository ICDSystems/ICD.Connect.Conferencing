using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Calendaring;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Calender
{
    public sealed class CiscoBooking : AbstractBooking
    {
	    private readonly Booking m_Booking;
	    private readonly List<IBookingNumber> m_BookingNumbers;

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

	    public override IEnumerable<IBookingNumber> GetBookingNumbers()
	    {
			return m_BookingNumbers.ToArray(m_BookingNumbers.Count);
		}

	    public override eMeetingType Type
	    {
		    get
		    {
		        return m_Booking.WebexEnabled
		            ? eMeetingType.VideoConference
		            : m_Booking.GetCalls()
		                       .Select(c => FromCallType(c.CallType))
		                       .FirstOrDefault(eMeetingType.Presentation);
		    }
	    }

	    public CiscoBooking(Booking booking)
	    {
		    m_Booking = booking;
		    m_BookingNumbers = ParseBookingNumbers().ToList();
	    }

        private static eMeetingType FromCallType(eCallType type)
        {
            switch (type)
            {
                case eCallType.Unknown:
                case eCallType.Video:
                    return eMeetingType.VideoConference;

                case eCallType.Audio:
                case eCallType.AudioCanEscalate:
                case eCallType.ForwardAllCall:
                    return eMeetingType.AudioConference;

                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

	    private IEnumerable<IBookingNumber> ParseBookingNumbers()
	    {
		    foreach (BookingCall call in m_Booking.GetCalls())
		    {
			    switch (call.Protocol.ToUpper())
			    {
				    case "SIP":
					    yield return new SipBookingNumber(call.Number);
					    continue;
			    }
			}

	    }
    }
}
