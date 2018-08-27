using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Comparers;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Calendaring_NetStandard;
using ICD.Connect.Calendaring_NetStandard.CalendarControl;
using ICD.Connect.Conferencing.Zoom.Components.Bookings;

namespace ICD.Connect.Conferencing.Zoom.Controls.Calendar
{
    public sealed class ZoomRoomCalendarControl : AbstractCalendarControl<ZoomRoom>
    {
	    private readonly BookingsComponent m_BookingsComponent;

	    private readonly List<ZoomBooking> m_SortedBookings;
	    private readonly Dictionary<string, ZoomBooking> m_MeetingNumberToBooking;

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
			m_SortedBookings = new List<ZoomBooking>();
			m_MeetingNumberToBooking = new Dictionary<string, ZoomBooking>();

		    m_BookingsComponent = Parent.Components.GetComponent<BookingsComponent>();
		    Subscribe(m_BookingsComponent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
	    protected override void DisposeFinal(bool disposing)
	    {
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

		    Booking[] bookings = m_BookingsComponent.GetBookings().ToArray();
		    IcdHashSet<string> existing = m_SortedBookings.Select(b => b.MeetingNumber).ToIcdHashSet();
		    IcdHashSet<string> current = bookings.Select(b => b.MeetingNumber).ToIcdHashSet();

		    IcdHashSet<string> removeMeetingNumberList = existing.Subtract(current);
		    foreach (string meetingNumber in removeMeetingNumberList)
			    change |= RemoveBooking(meetingNumber);

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

		    if (m_MeetingNumberToBooking.ContainsKey(booking.MeetingNumber))
			    return false;

		    ZoomBooking zoomBooking = new ZoomBooking(booking);
		    m_MeetingNumberToBooking[booking.MeetingNumber] = zoomBooking;


		    m_SortedBookings.AddSorted(zoomBooking, s_BookingComparer);

		    return true;
	    }

	    private bool RemoveBooking(string meetingNumber)
	    {
		    ZoomBooking zoomBooking;
		    if (!m_MeetingNumberToBooking.TryGetValue(meetingNumber, out zoomBooking))
			    return false;

		    m_MeetingNumberToBooking.Remove(meetingNumber);
		    m_SortedBookings.Remove(zoomBooking);

		    return true;
	    }
    }
}
