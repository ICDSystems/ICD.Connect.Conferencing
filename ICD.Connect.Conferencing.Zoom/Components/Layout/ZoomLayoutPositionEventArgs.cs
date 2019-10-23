using System;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Layout
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
