using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Calendaring;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Utils
{
	public static class ConferencingBookingUtils
	{
		/// <summary>
		/// Gets the best dialer for the booking based on what is supported by the given dialers.
		/// </summary>
		/// <param name="booking"></param>
		/// <param name="dialers"></param>
		/// <param name="number"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IDialingDeviceControl GetBestDialer(IBooking booking, IEnumerable<IDialingDeviceControl> dialers,
		                                                  out IBookingNumber number)
		{
			if (booking == null)
				throw new ArgumentNullException("booking");

			if (dialers == null)
				throw new ArgumentNullException("dialers");

			// Build map of dialer to best number
			Dictionary<IDialingDeviceControl, IBookingNumber> map = dialers.ToDictionary(d => d, d => GetBestNumber(d, booking));

			IDialingDeviceControl output = map.Keys
			                                  .Where(d => map.GetDefault(d) != null)
			                                  .OrderByDescending(d => d.CanDial(map[d]))
			                                  .ThenByDescending(d => d.Supports)
			                                  .FirstOrDefault();

			number = output == null ? null : map.GetDefault(output);

			return output;
		}

		/// <summary>
		/// Gets the meeting type for the booking based on what is supported by the given dialers.
		/// </summary>
		/// <param name="booking"></param>
		/// <param name="dialers"></param>
		/// <returns></returns>
		public static eMeetingType GetMeetingType(IBooking booking, IEnumerable<IDialingDeviceControl> dialers)
		{
			if (booking == null)
				throw new ArgumentNullException("booking");

			if (dialers == null)
				throw new ArgumentNullException("dialers");

			IBookingNumber bookingNumber;
			IDialingDeviceControl preferredDialer = GetBestDialer(booking, dialers, out bookingNumber);

			if (preferredDialer == null)
				return eMeetingType.Presentation;

			eMeetingType meetingType = bookingNumber.Protocol.ToMeetingType();

			// Get the intersection of the supported conference source types against the booking source types
			eConferenceSourceType supported = preferredDialer.Supports;
			eConferenceSourceType meetingSourceType = GetConferenceSourceType(meetingType);
			eConferenceSourceType intersection = EnumUtils.GetFlagsIntersection(supported, meetingSourceType);

			// Convert back to meeting type
			return GetMeetingType(intersection);
		}

		/// <summary>
		/// Converts the conference source type to a meeting type.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns></returns>
		private static eMeetingType GetMeetingType(eConferenceSourceType sourceType)
		{
			if (sourceType.HasFlag(eConferenceSourceType.Video))
				return eMeetingType.VideoConference;
			if (sourceType.HasFlag(eConferenceSourceType.Audio))
				return eMeetingType.AudioConference;

			return eMeetingType.Presentation;
		}

		/// <summary>
		/// Returns the booking number from the given booking that is best supported by the given dialer.
		/// </summary>
		/// <param name="dialer"></param>
		/// <param name="booking"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IBookingNumber GetBestNumber(IDialingDeviceControl dialer, IBooking booking)
		{
			if (dialer == null)
				throw new ArgumentNullException("dialer");

			if (booking == null)
				throw new ArgumentNullException("booking");

			return booking.GetBookingNumbers()
			              .OrderByDescending(n => dialer.CanDial(n))
			              .FirstOrDefault();
		}

		public static eConferenceSourceType GetConferenceSourceType(eBookingProtocol protocol)
		{
			return GetConferenceSourceType(protocol.ToMeetingType());
		}

		public static eConferenceSourceType GetConferenceSourceType(eMeetingType meetingType)
		{
			switch (meetingType)
			{
				case eMeetingType.AudioConference:
					return eConferenceSourceType.Audio;
				case eMeetingType.VideoConference:
					return eConferenceSourceType.Audio | eConferenceSourceType.Video;
				case eMeetingType.Presentation:
					return eConferenceSourceType.Unknown;

				default:
					throw new ArgumentOutOfRangeException("meetingType");
			}
		}
	}
}
