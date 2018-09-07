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
using ICD.Connect.Calendaring.Comparers;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Calender
{
    public sealed class CiscoCalendarControl : AbstractCalendarControl<CiscoCodecDevice>
    {
        private const int TIMERREFRESHINTERVAL = 10 * 60 * 1000;

        private readonly BookingsComponent m_BookingsComponent;
	    private readonly SafeTimer m_RefreshTimer;
	    private readonly List<CiscoBooking> m_SortedBookings;
	    private readonly IcdHashSet<CiscoBooking> m_HashBooking;

	    /// <summary>
	    /// Raised when bookings are added/removed.
	    /// </summary>
	    public override event EventHandler OnBookingsChanged;

		/// <summary>
		/// Sort bookings by start time.
		/// </summary>
		private static readonly PredicateComparer<CiscoBooking, DateTime> s_BookingComparer;

		/// <summary>
		/// Static constructor.
		/// </summary>
		static CiscoCalendarControl()
	    {
			s_BookingComparer = new PredicateComparer<CiscoBooking, DateTime>(b => b.StartTime);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCalendarControl(CiscoCodecDevice parent, int id)
		    : base(parent, id)
	    {
		    m_RefreshTimer = new SafeTimer(Refresh, TIMERREFRESHINTERVAL, TIMERREFRESHINTERVAL);

		    m_SortedBookings = new List<CiscoBooking>();
		    m_HashBooking = new IcdHashSet<CiscoBooking>(new BookingsComparer<CiscoBooking>());

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

		    IcdHashSet<CiscoBooking> existing = m_SortedBookings.ToIcdHashSet(new BookingsComparer<CiscoBooking>());
		    IcdHashSet<CiscoBooking> current = bookings.Select(b => new CiscoBooking(b)).ToIcdHashSet(new BookingsComparer<CiscoBooking>());

		    IcdHashSet<CiscoBooking> removeBookingList = existing.Subtract(current);
		    foreach (CiscoBooking booking in removeBookingList)
			    change |= RemoveBooking(booking);

		    foreach (var booking in bookings)
			    change |= AddBooking(booking);

		    if (change)
			    OnBookingsChanged.Raise(this);

	    }

	    public override void Refresh()
	    {
			m_BookingsComponent.ListBookings();
	    }

		public override IEnumerable<IBooking> GetBookings()
	    {
		    return m_SortedBookings.ToArray(m_SortedBookings.Count);
	    }

	    private bool AddBooking(Booking booking)
	    {
		    if (booking == null)
			    throw new ArgumentNullException("booking");

		    CiscoBooking ciscoBooking = new CiscoBooking(booking);

		    if (m_HashBooking.Contains(ciscoBooking))
			    return false;

		    m_HashBooking.Add(ciscoBooking);

		    m_SortedBookings.AddSorted(ciscoBooking, s_BookingComparer);

		    return true;
	    }

	    private bool RemoveBooking(CiscoBooking ciscoBooking)
	    {
		    if (!m_HashBooking.Contains(ciscoBooking))
			    return false;

		    m_HashBooking.Remove(ciscoBooking);
		    m_SortedBookings.Remove(ciscoBooking);

		    return true;
	    }
    }
}
