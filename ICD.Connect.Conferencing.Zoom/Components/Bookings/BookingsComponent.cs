using System;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Bookings
{
	public class BookingsComponent : AbstractZoomRoomComponent
	{
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

		protected override void Initialize()
		{
			base.Initialize();

			Parent.SendCommand("zCommand Bookings Update");
			Parent.SendCommand("zCommand Bookings List");
		}

		#endregion

		#region Zoom Room Callbacks

		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<BookingsListCommandResponse>(ListBookingsCallback);
		}

		private void Unsubscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.UnregisterResponseCallback<BookingsListCommandResponse>(ListBookingsCallback);
		}

		private void ListBookingsCallback(ZoomRoom zoomRoom, BookingsListCommandResponse response)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}