using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
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
		public static IDialingDeviceControl GetBestDialer(IBooking booking, IEnumerable<IDialingDeviceControl> dialers, out IBookingNumber number)
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

			// Build map of dialer to best number
			Dictionary<IDialingDeviceControl, IBookingNumber> map = dialers.ToDictionary(d => d, d => GetBestNumber(d, booking));

			IDialingDeviceControl preferredDialer = map.Keys
			                                           .Where(d => map.GetDefault(d) != null)
			                                           .OrderByDescending(d => d.CanDial(map[d]))
			                                           .ThenByDescending(d => d.Supports)
			                                           .FirstOrDefault();

			if (preferredDialer == null)
				return eMeetingType.Presentation;

			if (preferredDialer.Supports.HasFlag(eConferenceSourceType.Video))
			{
				eMeetingType conferenceSourceType = ToConferenceSourceType(map.GetDefault(preferredDialer).Protocol);
				return eMeetingType.VideoConference == conferenceSourceType ? eMeetingType.VideoConference : conferenceSourceType;
			}


			if (preferredDialer.Supports.HasFlag(eConferenceSourceType.Audio))
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

		/// <summary>
		/// Gets the meeting type for the given booking protocol.
		/// </summary>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public static eMeetingType ToConferenceSourceType(eBookingProtocol protocol)
		{
			switch (protocol)
			{
				case eBookingProtocol.Zoom:
				case eBookingProtocol.Sip:
					return eMeetingType.VideoConference;

				case eBookingProtocol.Pstn:
					return eMeetingType.AudioConference;

				case eBookingProtocol.None:
					return eMeetingType.Presentation;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
