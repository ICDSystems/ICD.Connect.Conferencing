using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Comparers;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Calendaring.Booking;
using ICD.Connect.Calendaring.Controls;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Calendar;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls.Calender
{
    public sealed class PolycomCalendarControl : AbstractCalendarControl<PolycomGroupSeriesDevice>
    {
	    private const int REFRESH_INTERVAL = 10 * 60 * 1000;

	    private const string TODAY = "today";
        private const string TOMORROW = "tomorrow";

	    /// <summary>
	    /// Raised when bookings are added/removed.
	    /// </summary>
	    public override event EventHandler OnBookingsChanged;

	    private readonly CalendarComponent m_BookingsComponent;
	    private readonly SafeTimer m_RefreshTimer;
		private readonly IcdOrderedDictionary<MeetingInfo, PolycomBooking> m_MeetingInfoToBooking;
	    private readonly SafeCriticalSection m_CriticalSection;

	    /// <summary>
		/// Sort meeting infos by start time.
		/// </summary>
		private static readonly PredicateComparer<MeetingInfo, DateTime> s_MeetingInfoComparer;

		/// <summary>
		/// Static constructor.
		/// </summary>
		static PolycomCalendarControl()
	    {
			s_MeetingInfoComparer = new PredicateComparer<MeetingInfo, DateTime>(m => m.Start);
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCalendarControl(PolycomGroupSeriesDevice parent, int id)
		    : base(parent, id)
	    {
		    m_RefreshTimer = new SafeTimer(Refresh, REFRESH_INTERVAL, REFRESH_INTERVAL);

		    m_MeetingInfoToBooking = new IcdOrderedDictionary<MeetingInfo, PolycomBooking>(s_MeetingInfoComparer);
			m_CriticalSection = new SafeCriticalSection();

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

	    #region Methods

	    public override void Refresh()
	    {
			m_BookingsComponent.CalendarList(TODAY, TOMORROW);
	    }

		public override IEnumerable<IBooking> GetBookings()
	    {
		    return m_CriticalSection.Execute(() => m_MeetingInfoToBooking.Values.ToArray(m_MeetingInfoToBooking.Count));
	    }

	    #endregion

	    #region Private Methods

	    private bool AddBooking(MeetingInfo meeting)
	    {
		    if (meeting == null)
			    throw new ArgumentNullException("booking");

			m_CriticalSection.Enter();

		    try
		    {
				if (m_MeetingInfoToBooking.ContainsKey(meeting))
					return false;

				PolycomBooking polycomBooking = new PolycomBooking(meeting);
				m_MeetingInfoToBooking.Add(meeting, polycomBooking);
		    }
		    finally
		    {
			    m_CriticalSection.Leave();
		    }

		    return true;
	    }

		private bool RemoveBooking(MeetingInfo meeting)
		{
			return m_CriticalSection.Execute(() => m_MeetingInfoToBooking.Remove(meeting));
		}

	    #endregion

	    #region Component Callbacks

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

		    MeetingInfo[] meetings = m_BookingsComponent.GetMeetingInfos()
		                                                .Where(b => b.End > IcdEnvironment.GetLocalTime())
		                                                .Distinct()
		                                                .ToArray();

			m_CriticalSection.Enter();

		    try
		    {
				IcdHashSet<MeetingInfo> existing = m_MeetingInfoToBooking.Keys.ToIcdHashSet();
				IcdHashSet<MeetingInfo> removeBookingList = existing.Subtract(meetings);

				foreach (MeetingInfo meeting in removeBookingList)
					change |= RemoveBooking(meeting);

				foreach (MeetingInfo meeting in meetings)
					change |= AddBooking(meeting);
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
