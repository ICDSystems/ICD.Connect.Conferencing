using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Comparers;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Calendaring.CalendarControl;
using ICD.Connect.Conferencing.Zoom.Comparers;
using ICD.Connect.Conferencing.Zoom.Components.Bookings;

namespace ICD.Connect.Conferencing.Zoom.Controls.Calendar
{
    public sealed class ZoomRoomCalendarControl : AbstractCalendarControl<ZoomRoom>
    {
	    private readonly BookingsComponent m_BookingsComponent;
	    private readonly SafeTimer m_RefreshTimer;
	    private readonly List<ZoomBooking> m_SortedBookings;
	    private readonly IcdHashSet<ZoomBooking> m_HashBooking;

	    /// <summary>
	    /// Raised when bookings are added/removed.
	    /// </summary>
	    public override event EventHandler OnBookingsChanged;

		/// <summary>
		/// Sort bookings by start time.
		/// </summary>
		private static readonly PredicateComparer<ZoomBooking, DateTime> s_BookingComparer;

		/// <summary>
		/// Static constructor.
		/// </summary>
		static ZoomRoomCalendarControl()
	    {
			s_BookingComparer = new PredicateComparer<ZoomBooking, DateTime>(b => b.StartTime);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ZoomRoomCalendarControl(ZoomRoom parent, int id)
		    : base(parent, id)
	    {
		    m_RefreshTimer = new SafeTimer(Refresh, 600000);

		    m_SortedBookings = new List<ZoomBooking>();
		    m_HashBooking = new IcdHashSet<ZoomBooking>(new BookingsComparer());

		    m_BookingsComponent = Parent.Components.GetComponent<BookingsComponent>();
		    Subscribe(m_BookingsComponent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
	    protected override void DisposeFinal(bool disposing)
		{
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
		    bookings.OnBookingsUpdated += BookingsOnOnBookingsUpdated;
	    }

		/// <summary>
		/// Unsubscribe from the bookings events.
		/// </summary>
		/// <param name="bookings"></param>
	    private void Unsubscribe(BookingsComponent bookings)
	    {
		    bookings.OnBookingsUpdated -= BookingsOnOnBookingsUpdated;
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
		    IcdHashSet<ZoomBooking> existing = m_SortedBookings.ToIcdHashSet(new BookingsComparer());
		    IcdHashSet<ZoomBooking> current = bookings.Select(b => new ZoomBooking(b)).ToIcdHashSet(new BookingsComparer());

		    IcdHashSet<ZoomBooking> removeBookingList = existing.Subtract(current);
		    foreach (ZoomBooking booking in removeBookingList)
			    change |= RemoveBooking(booking);

		    foreach (var booking in bookings)
			    change |= AddBooking(booking);

		    if (change)
			    OnBookingsChanged.Raise(this);

	    }

	    public override void Refresh()
	    {
			m_BookingsComponent.UpdateBookings();
	    }

		public override IEnumerable<IBooking> GetBookings()
	    {
		    return m_SortedBookings.ToArray(m_SortedBookings.Count);
	    }

	    private bool AddBooking(Booking booking)
	    {
		    if (booking == null)
			    throw new ArgumentNullException("booking");

		    ZoomBooking zoomBooking = new ZoomBooking(booking);

		    if (m_HashBooking.Contains(zoomBooking))
			    return false;

		    m_HashBooking.Add(zoomBooking);

		    m_SortedBookings.AddSorted(zoomBooking, s_BookingComparer);

		    return true;
	    }

	    private bool RemoveBooking(ZoomBooking zoomBooking)
	    {
		    if (!m_HashBooking.Contains(zoomBooking))
			    return false;

		    m_HashBooking.Remove(zoomBooking);
		    m_SortedBookings.Remove(zoomBooking);

		    return true;
	    }
    }
}
