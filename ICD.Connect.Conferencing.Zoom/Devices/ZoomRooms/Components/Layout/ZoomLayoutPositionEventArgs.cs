using System;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Layout
{
	public sealed class ZoomLayoutPositionEventArgs : EventArgs
	{
		private readonly eZoomLayoutPosition m_LayoutPosition;

		public eZoomLayoutPosition LayoutPosition { get { return m_LayoutPosition; } }

		public ZoomLayoutPositionEventArgs(eZoomLayoutPosition layoutPosition)
		{
			m_LayoutPosition = layoutPosition;
		}
	}
}
