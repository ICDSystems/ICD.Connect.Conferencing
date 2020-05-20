using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Comparers;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Calendaring.Bookings;
using ICD.Connect.Calendaring.Controls;
using ICD.Connect.Conferencing.Zoom.Components.Bookings;
using Booking = ICD.Connect.Conferencing.Zoom.Components.Bookings.Booking;

namespace ICD.Connect.Conferencing.Zoom.Controls.Calendar
{
    public sealed class ZoomRoomCalendarControl : AbstractCalendarControl<ZoomRoom>
    {
        private const int REFRESH_INTERVAL = 10 * 60 * 1000;

	    /// <summary>
	    /// Raised when bookings are added/removed.
	    /// </summary>
	    public override event EventHandler OnBookingsChanged;

	    private readonly BookingsComponent m_BookingsComponent;
	    private readonly SafeTimer m_RefreshTimer;
		private readonly IcdOrderedDictionary<Booking, ZoomBooking> m_BookingToZoomBooking;
	    private readonly SafeCriticalSection m_CriticalSection;

	    /// <summary>
		/// Sort bookings by start time.
		/// </summary>
		private static readonly PredicateComparer<Booking, DateTime> s_BookingComparer;

		/// <summary>
		/// Static constructor.
		/// </summary>
		static ZoomRoomCalendarControl()
	    {
			s_BookingComparer = new PredicateComparer<Booking, DateTime>(b => b.StartTime);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ZoomRoomCalendarControl(ZoomRoom parent, int id)
		    : base(parent, id)
	    {
		    m_BookingToZoomBooking = new IcdOrderedDictionary<Booking, ZoomBooking>(s_BookingComparer);
			m_CriticalSection = new SafeCriticalSection();

		    m_BookingsComponent = Parent.Components.GetComponent<BookingsComponent>();
		    Subscribe(m_BookingsComponent);

			m_RefreshTimer = new SafeTimer(Refresh, REFRESH_INTERVAL, REFRESH_INTERVAL);
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
		    m_BookingsComponent.UpdateBookings();
	    }

		public override IEnumerable<IBooking> GetBookings()
	    {
			return m_CriticalSection.Execute(() => m_BookingToZoomBooking.Values.ToArray(m_BookingToZoomBooking.Count));
	    }

		/// <summary>
		/// Returns true if the booking argument can be checked in.
		/// </summary>
		/// <returns></returns>
		public override bool CanCheckIn(IBooking booking)
		{
			return booking is ZoomBooking && !booking.CheckedIn;
		}

		public override bool CanCheckOut(IBooking booking)
		{
			return false;
		}

		/// <summary>
		/// Checks in to the specified booking.
		/// </summary>
		/// <param name="booking"></param>
		public override void CheckIn(IBooking booking)
		{
			if (!CanCheckIn(booking))
				throw new ArgumentException("The specified booking does not support check ins.", "booking");

			m_BookingsComponent.CheckIn(booking.MeetingName);
		}

		public override void CheckOut(IBooking booking)
		{
			throw new NotSupportedException();
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
				if (m_BookingToZoomBooking.ContainsKey(booking))
					return false;

				ZoomBooking zoomBooking = new ZoomBooking(booking);
				m_BookingToZoomBooking.Add(booking, zoomBooking);
		    }
		    finally
		    {
			    m_CriticalSection.Leave();
		    }

		    return true;
	    }

	    private bool RemoveBooking(Booking zoomBooking)
	    {
		    return m_CriticalSection.Execute(() => m_BookingToZoomBooking.Remove(zoomBooking));
	    }

	    #endregion

	    #region Component Callbacks

	    /// <summary>
	    /// Subscribe to the bookings events.
	    /// </summary>
	    /// <param name="bookings"></param>
	    private void Subscribe(BookingsComponent bookings)
	    {
		    bookings.OnBookingsUpdated += BookingsOnBookingsUpdated;
	    }

	    /// <summary>
	    /// Unsubscribe from the bookings events.
	    /// </summary>
	    /// <param name="bookings"></param>
	    private void Unsubscribe(BookingsComponent bookings)
	    {
		    bookings.OnBookingsUpdated -= BookingsOnBookingsUpdated;
	    }

	    /// <summary>
	    /// Called when bookings are added/removed.
	    /// </summary>
	    /// <param name="sender"></param>
	    /// <param name="e"></param>
	    private void BookingsOnBookingsUpdated(object sender, EventArgs e)
	    {
		    bool change = false;

		    Booking[] bookings = m_BookingsComponent.GetBookings()
		                                            .Where(b => b.EndTime > IcdEnvironment.GetUtcTime())
		                                            .Distinct()
		                                            .ToArray();

		    m_CriticalSection.Enter();

		    try
		    {
			    IcdHashSet<Booking> existing = m_BookingToZoomBooking.Keys.ToIcdHashSet();
			    IcdHashSet<Booking> removeBookingList = existing.Subtract(bookings);

			    foreach (Booking booking in removeBookingList)
				    change |= RemoveBooking(booking);

			    foreach (Booking booking in bookings)
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
