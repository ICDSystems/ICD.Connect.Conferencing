using System;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Layout
{
	public sealed class ZoomLayoutStyleEventArgs : EventArgs
	{
		private readonly eZoomLayoutStyle m_LayoutStyle;

		public eZoomLayoutStyle LayoutStyle { get { return m_LayoutStyle; } }

		public ZoomLayoutStyleEventArgs(eZoomLayoutStyle layoutStyle)
		{
			m_LayoutStyle = layoutStyle;
		}
	}
}
