using System;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Layout
{
	public sealed class ZoomLayoutSizeEventArgs : EventArgs
	{
		private readonly eZoomLayoutSize m_LayoutSize;

		public eZoomLayoutSize LayoutSize { get { return m_LayoutSize; } }

		public ZoomLayoutSizeEventArgs(eZoomLayoutSize layoutSize)
		{
			m_LayoutSize = layoutSize;
		}
	}
}
