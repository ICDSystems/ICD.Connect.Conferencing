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
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Calendar;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls.Calender
{
    public sealed class PolycomCalendarControl : AbstractCalendarControl<PolycomGroupSeriesDevice>
    {
        private const string TODAY = "today";
        private const string TOMORROW = "tomorrow";
        private const int TIMERREFRESHINTERVAL = 10 * 60 * 1000;

        private readonly CalendarComponent m_BookingsComponent;
	    private readonly SafeTimer m_RefreshTimer;
	    private readonly List<PolycomBooking> m_SortedBookings;
	    private readonly IcdHashSet<PolycomBooking> m_HashBooking;

	    /// <summary>
	    /// Raised when bookings are added/removed.
	    /// </summary>
	    public override event EventHandler OnBookingsChanged;

		/// <summary>
		/// Sort bookings by start time.
		/// </summary>
		private static readonly PredicateComparer<PolycomBooking, DateTime> s_BookingComparer;

		/// <summary>
		/// Static constructor.
		/// </summary>
		static PolycomCalendarControl()
	    {
			s_BookingComparer = new PredicateComparer<PolycomBooking, DateTime>(b => b.StartTime);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCalendarControl(PolycomGroupSeriesDevice parent, int id)
		    : base(parent, id)
	    {
		    m_RefreshTimer = new SafeTimer(Refresh, TIMERREFRESHINTERVAL, TIMERREFRESHINTERVAL);

		    m_SortedBookings = new List<PolycomBooking>();
		    m_HashBooking = new IcdHashSet<PolycomBooking>(new BookingsComparer<PolycomBooking>());

		    m_BookingsComponent = Parent.Components.GetComponent<CalendarComponent>();
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
	    private void Subscribe(CalendarComponent bookings)
	    {
		    bookings.OnMeetingsChanged += BookingsOnOnBookingsUpdated;
	    }

		/// <summary>
		/// Unsubscribe from the bookings events.
		/// </summary>
		/// <param name="bookings"></param>
	    private void Unsubscribe(CalendarComponent bookings)
	    {
		    bookings.OnMeetingsChanged -= BookingsOnOnBookingsUpdated;
	    }

	    /// <summary>
	    /// Called when bookings are added/removed.
	    /// </summary>
	    /// <param name="sender"></param>
	    /// <param name="e"></param>
	    private void BookingsOnOnBookingsUpdated(object sender, EventArgs e)
	    {
		    bool change = false;

	        MeetingInfo[] bookings = m_BookingsComponent.GetMeetingInfos()
	                                                    .Where(b => b.End > IcdEnvironment.GetLocalTime())
	                                                    .Distinct()
	                                                    .ToArray();

		    IcdHashSet<PolycomBooking> existing = m_SortedBookings.ToIcdHashSet(new BookingsComparer<PolycomBooking>());
		    IcdHashSet<PolycomBooking> current = bookings.Select(b => new PolycomBooking(b)).ToIcdHashSet(new BookingsComparer<PolycomBooking>());

		    IcdHashSet<PolycomBooking> removeBookingList = existing.Subtract(current);
		    foreach (PolycomBooking booking in removeBookingList)
			    change |= RemoveBooking(booking);

		    foreach (var booking in bookings)
			    change |= AddBooking(booking);

		    if (change)
			    OnBookingsChanged.Raise(this);

	    }

	    public override void Refresh()
	    {
			m_BookingsComponent.CalendarList(TODAY, TOMORROW);
	    }

		public override IEnumerable<IBooking> GetBookings()
	    {
		    return m_SortedBookings.ToArray(m_SortedBookings.Count);
	    }

	    private bool AddBooking(MeetingInfo booking)
	    {
		    if (booking == null)
			    throw new ArgumentNullException("booking");

		    PolycomBooking polycomBooking = new PolycomBooking(booking);

		    if (m_HashBooking.Contains(polycomBooking))
			    return false;

		    m_HashBooking.Add(polycomBooking);

		    m_SortedBookings.AddSorted(polycomBooking, s_BookingComparer);

		    return true;
	    }

	    private bool RemoveBooking(PolycomBooking polycomBooking)
	    {
		    if (!m_HashBooking.Contains(polycomBooking))
			    return false;

		    m_HashBooking.Remove(polycomBooking);
		    m_SortedBookings.Remove(polycomBooking);

		    return true;
	    }
    }
}
