using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Component
{
	public class ZoomBookingsCache
	{
		private readonly ZoomRoom m_ZoomRoom;

		public ZoomBookingsCache(ZoomRoom zoomRoom)
		{
			m_ZoomRoom = zoomRoom;
			Subscribe(m_ZoomRoom);
		}

		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<BookingsListCommandResponse>(ListBookingsCallback);
		}

		private void ListBookingsCallback(ZoomRoom zoomRoom, BookingsListCommandResponse response)
		{
			throw new NotImplementedException();
		}
	}
}