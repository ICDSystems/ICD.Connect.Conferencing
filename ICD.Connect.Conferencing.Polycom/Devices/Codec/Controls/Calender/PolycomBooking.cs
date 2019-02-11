using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Calendar;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls.Calender
{
	public sealed class PolycomBooking : AbstractBooking
	{
		private readonly MeetingInfo m_Booking;
		private readonly List<IDialContext> m_BookingNumbers;

		public override string MeetingName
		{
			get { return m_Booking.Subject; }
		}

		public override string OrganizerName
		{
			get { return m_Booking.Organizer; }
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

		public override IEnumerable<IDialContext> GetBookingNumbers()
		{
			return m_BookingNumbers.ToArray(m_BookingNumbers.Count);
		}

		public PolycomBooking(MeetingInfo booking)
		{
			m_Booking = booking;
			m_BookingNumbers = ParseBookingNumbers().ToList();
		}

		private IEnumerable<IDialContext> ParseBookingNumbers()
		{
			foreach (MeetingInfo.DialingNumber number in m_Booking.GetDialingNumbers())
			{
				switch (number.Protocol.ToUpper())
				{
					case "SIP":
						yield return new SipDialContext { DialString = number.Number };
						continue;
				}
			}
		}
	}
}
