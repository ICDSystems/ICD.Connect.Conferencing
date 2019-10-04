using System;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Layout
{
	public sealed class LayoutConfigurationEventArgs : EventArgs
	{
		private readonly bool m_ShareThumb;
		private readonly eZoomLayoutStyle m_LayoutStyle;
		private readonly eZoomLayoutSize m_LayoutSize;
		private readonly eZoomLayoutPosition m_LayoutPosition;

		public bool ShareThumb { get { return m_ShareThumb; } }

		public eZoomLayoutStyle LayoutStyle { get { return m_LayoutStyle; } }

		public eZoomLayoutSize LayoutSize { get { return m_LayoutSize; } }

		public eZoomLayoutPosition LayoutPosition { get { return m_LayoutPosition; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="selfView"></param>
		/// <param name="shareThumb"></param>
		/// <param name="layoutStyle"></param>
		/// <param name="layoutSize"></param>
		/// <param name="layoutPosition"></param>
		public LayoutConfigurationEventArgs(bool shareThumb, eZoomLayoutStyle layoutStyle, eZoomLayoutSize layoutSize,
		                                    eZoomLayoutPosition layoutPosition)
		{
			m_ShareThumb = shareThumb;
			m_LayoutStyle = layoutStyle;
			m_LayoutSize = layoutSize;
			m_LayoutPosition = layoutPosition;
		}
	}
}
