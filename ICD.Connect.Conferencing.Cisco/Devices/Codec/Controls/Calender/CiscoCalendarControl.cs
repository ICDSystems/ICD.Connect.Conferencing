using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Comparers;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Calendaring.CalendarControl;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Calender
{
	public sealed class CiscoCalendarControl : AbstractCalendarControl<CiscoCodecDevice>
	{
		private const int REFRESH_INTERVAL = 10 * 60 * 1000;

		/// <summary>
		/// Raised when bookings are added/removed.
		/// </summary>
		public override event EventHandler OnBookingsChanged;

		private readonly BookingsComponent m_BookingsComponent;
		private readonly SafeTimer m_RefreshTimer;
		private readonly IcdOrderedDictionary<Booking, CiscoBooking> m_BookingToCiscoBookings;
		private readonly SafeCriticalSection m_CriticalSection;

		/// <summary>
		/// Sort bookings by start time.
		/// </summary>
		private static readonly PredicateComparer<Booking, DateTime> s_BookingComparer;

		/// <summary>
		/// Compare bookings by id.
		/// </summary>
		private static readonly PredicateEqualityComparer<Booking, int> s_BookingEqualityComparer;

		/// <summary>
		/// Static constructor.
		/// </summary>
		static CiscoCalendarControl()
		{
			s_BookingComparer = new PredicateComparer<Booking, DateTime>(b => b.StartTime);
			s_BookingEqualityComparer = new PredicateEqualityComparer<Booking, int>(b => b.Id);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCalendarControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_RefreshTimer = new SafeTimer(Refresh, REFRESH_INTERVAL, REFRESH_INTERVAL);

			m_BookingToCiscoBookings = new IcdOrderedDictionary<Booking, CiscoBooking>(s_BookingComparer,
			                                                                           s_BookingEqualityComparer);
			m_CriticalSection = new SafeCriticalSection();

			m_BookingsComponent = Parent.Components.GetComponent<BookingsComponent>();
			Subscribe(m_BookingsComponent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnBookingsChanged = null;

			m_RefreshTimer.Dispose();

			base.DisposeFinal(disposing);

			Unsubscribe(m_BookingsComponent);
		}

		#region Methods

		public override void Refresh()
		{
			m_BookingsComponent.ListBookings();
		}

		public override IEnumerable<IBooking> GetBookings()
		{
			return m_CriticalSection.Execute(() => m_BookingToCiscoBookings.Values.ToArray(m_BookingToCiscoBookings.Count));
		}

		#endregion

		#region Private Methods

		private bool AddBooking(Booking booking)
		{
			if (booking == null)
				throw new ArgumentNullException("booking");

			m_CriticalSection.Enter();

			try
			{
				if (m_BookingToCiscoBookings.ContainsKey(booking))
					return false;

				CiscoBooking ciscoBooking = new CiscoBooking(booking);
				m_BookingToCiscoBookings.Add(booking, ciscoBooking);
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			return true;
		}

		private bool RemoveBooking(Booking booking)
		{
			if (booking == null)
				throw new ArgumentNullException("booking");

			return m_CriticalSection.Execute(() => m_BookingToCiscoBookings.Remove(booking));
		}

		#endregion

		#region Component Callbacks

		/// <summary>
		/// Subscribe to the bookings events.
		/// </summary>
		/// <param name="bookings"></param>
		private void Subscribe(BookingsComponent bookings)
		{
			bookings.OnBookingsChanged += BookingsOnOnBookingsUpdated;
		}

		/// <summary>
		/// Unsubscribe from the bookings events.
		/// </summary>
		/// <param name="bookings"></param>
		private void Unsubscribe(BookingsComponent bookings)
		{
			bookings.OnBookingsChanged -= BookingsOnOnBookingsUpdated;
		}

		/// <summary>
		/// Called when bookings are added/removed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BookingsOnOnBookingsUpdated(object sender, EventArgs e)
		{
			bool change = false;

			Booking[] bookings = m_BookingsComponent.GetBookings()
			                                        .Where(b => b.EndTime > IcdEnvironment.GetLocalTime())
			                                        .Distinct()
			                                        .ToArray();

			m_CriticalSection.Enter();

			try
			{
				IcdHashSet<Booking> existing = m_BookingToCiscoBookings.Keys.ToIcdHashSet(s_BookingEqualityComparer);
				IcdHashSet<Booking> removeBookingList = existing.Subtract(bookings);

				foreach (Booking booking in removeBookingList)
					change |= RemoveBooking(booking);

				foreach (var booking in bookings)
					change |= AddBooking(booking);
			}
			finally
			{
				m_CriticalSection.Leave();
			}

			if (change)
				OnBookingsChanged.Raise(this);

		}

		#endregion
	}
}
