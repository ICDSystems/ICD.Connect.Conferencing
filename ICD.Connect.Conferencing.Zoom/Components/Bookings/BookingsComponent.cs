using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Bookings
{
	public class BookingsComponent : AbstractZoomRoomComponent
	{
		public event EventHandler OnBookingsUpdated;

		private readonly List<Booking> m_Bookings;

		public BookingsComponent(ZoomRoom zoomRoom)
			: base(zoomRoom)
		{
			Subscribe(zoomRoom);
		}

		protected override void DisposeFinal()
		{
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
			Parent.SendCommand("zCommand Bookings Update");
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
			zoomRoom.RegisterResponseCallback<BookingsListCommandResponse>(ListBookingsCallback);
		}

		private void Unsubscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.UnregisterResponseCallback<BookingsUpdateResponse>(BookingsUpdateCallback);
			zoomRoom.UnregisterResponseCallback<BookingsListCommandResponse>(ListBookingsCallback);
		}

		private void BookingsUpdateCallback(ZoomRoom zoomRoom, BookingsUpdateResponse response)
		{
			Parent.SendCommand("zCommand Bookings List");
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