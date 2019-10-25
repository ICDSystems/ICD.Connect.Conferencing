using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Bookings
{
	public sealed class BookingsComponent : AbstractZoomRoomComponent
	{
		public event EventHandler OnBookingsUpdated;

		private readonly List<Booking> m_Bookings;

		public BookingsComponent(ZoomRoom zoomRoom)
			: base(zoomRoom)
		{
			m_Bookings = new List<Booking>();

			Subscribe(zoomRoom);
		}

		protected override void DisposeFinal()
		{
			OnBookingsUpdated = null;

			base.DisposeFinal();

			Unsubscribe(Parent);
		}

		#region Methods

		/// <summary>
		/// Get the cached bookings for this Zoom Room
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Booking> GetBookings()
		{
			return m_Bookings.ToList();
		}

		/// <summary>
		/// Updates the bookings on the Zoom Room.
		/// </summary>
		/// <remarks>
		/// If tied to a Google Calendar, call this method no more than every 10 minutes,
		/// as many rooms polling the calendar can hit the query limit quickly and either
		/// cut off further queries or start charging the customer for queries.
		/// </remarks>
		public void UpdateBookings()
		{
			if (Initialized)
				Parent.SendCommand("zCommand Bookings Update");
		}

		public void ListBookings()
		{
			if (Initialized)
				Parent.SendCommand("zCommand Bookings List");
		}

		public void CheckIn(string meetingNumber)
		{
			if (!Initialized)
				return;

			Parent.Log(eSeverity.Informational, "Checking into meeting: {0}", meetingNumber);
			Parent.SendCommand("zCommand Dial CheckIn MeetingNumber: {0}", meetingNumber);
		}

		protected override void Initialize()
		{
			base.Initialize();

			UpdateBookings();
		}

		#endregion

		#region Zoom Room Callbacks

		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<BookingsUpdateResponse>(BookingsUpdateCallback);
			zoomRoom.RegisterResponseCallback<BookingsUpdatedEventResponse>(BookingsUpdatedEventCallback);
			zoomRoom.RegisterResponseCallback<BookingsListCommandResponse>(ListBookingsCallback);
		}

		private void Unsubscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.UnregisterResponseCallback<BookingsUpdateResponse>(BookingsUpdateCallback);
			zoomRoom.UnregisterResponseCallback<BookingsUpdatedEventResponse>(BookingsUpdatedEventCallback);
			zoomRoom.UnregisterResponseCallback<BookingsListCommandResponse>(ListBookingsCallback);
		}

		private void BookingsUpdateCallback(ZoomRoom zoomRoom, BookingsUpdateResponse response)
		{
			ListBookings();
		}

		private void BookingsUpdatedEventCallback(ZoomRoom zoomroom, BookingsUpdatedEventResponse response)
		{
			ListBookings();
		}

		private void ListBookingsCallback(ZoomRoom zoomRoom, BookingsListCommandResponse response)
		{
			m_Bookings.Clear();
			m_Bookings.AddRange(response.Bookings);

			OnBookingsUpdated.Raise(this);
		}

		#endregion
	}
}