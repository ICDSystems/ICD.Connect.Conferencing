using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings
{
	public sealed class BookingsComponent : AbstractCiscoComponent
	{
		/// <summary>
		/// Raised when items are added/removed to/from the bookings collection.
		/// </summary>
		public event EventHandler OnBookingsChanged;

		private readonly IcdOrderedDictionary<int, Booking> m_Bookings;
		private readonly SafeCriticalSection m_BookingsSection;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public BookingsComponent(CiscoCodecDevice codec)
			: base(codec)
		{
			m_Bookings = new IcdOrderedDictionary<int, Booking>();
			m_BookingsSection = new SafeCriticalSection();

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			OnBookingsChanged = null;

			base.Dispose(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the known bookings.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Booking> GetBookings()
		{
			return m_BookingsSection.Execute(() => m_Bookings.Values.ToArray(m_Bookings.Count));
		}

		/// <summary>
		/// Sends a command to the Cisco Codec to list the available bookings for the day.
		/// </summary>
		public void ListBookings()
		{
			ListBookings(1);
		}

		/// <summary>
		/// Sends a command to the Cisco Codec to list the available bookings.
		/// </summary>
		/// <param name="days">Number of days to retrieve bookings from (1..365).</param>
		public void ListBookings(int days)
		{
			ListBookings(days, 0, 65534, 0);
		}

		/// <summary>
		/// Sends a command to the Cisco Codec to list the available bookings.
		/// </summary>
		/// <param name="days">Number of days to retrieve bookings from (1..365).</param>
		/// <param name="dayOffset">Which day to start the search from (today: 0, tomorrow: 1…) (0..365).</param>
		/// <param name="limit">Max number of bookings to list (1..65534).</param>
		/// <param name="offset">Offset number of bookings for this search (0..65534).</param>
		public void ListBookings(int days, int dayOffset, int limit, int offset)
		{
			string command = "xCommand Bookings List";

			if (days != 0)
				command += string.Format(" Days:{0}", days);
			if (dayOffset != 0)
				command += string.Format(" DayOffset:{0}", dayOffset);
			if (limit != 0)
				command += string.Format(" Limit:{0}", limit);
			if (offset != 0)
				command += string.Format(" Offset:{0}", offset);

			Codec.SendCommand(command);
			Codec.Log(eSeverity.Informational, "Listing bookings for Days:{0} DayOffset:{1} Limit:{2} Offset:{3}", days, dayOffset, limit, offset);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			// Initial query to populate the bookings list
			ListBookings();
		}

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseBookingsList, "BookingsListResult");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseBookingsList, "BookingsListResult");
		}

		/// <summary>
		/// Parses the bookings list result.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="resultId"></param>
		/// <param name="xml"></param>
		private void ParseBookingsList(CiscoCodecDevice sender, string resultId, string xml)
		{
			bool change = false;

			IcdHashSet<Booking> bookings = XmlUtils.GetChildElementsAsString(xml, "Booking")
			                                       .Select(x => Booking.FromXml(x))
			                                       .ToIcdHashSet();

			IcdHashSet<int> bookingIds = bookings.Select(b => b.Id).ToIcdHashSet();

			m_BookingsSection.Enter();

			try
			{
				IcdHashSet<int> removeIds =
					m_Bookings.Keys
					          .ToIcdHashSet()
					          .Subtract(bookingIds);

				if (removeIds.Count > 0)
					change = true;

				m_Bookings.RemoveAll(removeIds);
			}
			finally
			{
				m_BookingsSection.Leave();
			}

			foreach (Booking booking in bookings)
				change |= UpdateBooking(booking);

			if (change)
				OnBookingsChanged.Raise(this);
		}

		/// <summary>
		/// Adds the booking to the collection, or updates the existing booking.
		/// </summary>
		/// <param name="booking"></param>
		/// <returns></returns>
		private bool UpdateBooking(Booking booking)
		{
			if (booking == null)
				throw new ArgumentNullException("booking");

			m_BookingsSection.Enter();

			try
			{
				Booking existing;
				if (m_Bookings.TryGetValue(booking.Id, out existing) && booking.Equals(existing))
					return false;

				m_Bookings[booking.Id] = booking;

				return true;
			}
			finally
			{
				m_BookingsSection.Leave();
			}
		}

		#endregion
	}
}
