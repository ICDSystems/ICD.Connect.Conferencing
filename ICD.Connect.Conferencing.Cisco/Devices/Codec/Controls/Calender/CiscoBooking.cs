using System;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Calender
{
    public sealed class CiscoBooking : AbstractBooking, ISipBooking
    {
	    private readonly Booking m_Booking;

	    public override string MeetingName
	    {
		    get { return m_Booking.Title; }
		}

	    public string SipUri
	    {
		    get
		    {
		        return m_Booking.WebexEnabled
		            ? m_Booking.WebexMeetingNumber
		            : m_Booking.GetCalls().Select(c => c.Number).FirstOrDefault();
		    }
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
    }
}
