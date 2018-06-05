using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;
using ICD.Connect.Conferencing.Controls.Layout;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecLayoutControl : AbstractConferenceLayoutControl<CiscoCodecDevice>
	{
		private readonly VideoComponent m_Video;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecLayoutControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_Video = parent.Components.GetComponent<VideoComponent>();
			Subscribe(m_Video);

			UpdateSelfView();
			UpdateSelfViewFullscreen();
			UpdateLayoutAvailable();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_Video);
		}

		#region Methods

		/// <summary>
		/// Enables/disables the self-view window during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetSelfViewEnabled(bool enabled)
		{
			m_Video.SetSelfViewEnabled(enabled);
		}

		/// <summary>
		/// Enables/disables the self-view fullscreen mode during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetSelfViewFullScreenEnabled(bool enabled)
		{
			m_Video.SetSelfViewFullScreen(enabled);
		}

		/// <summary>
		/// Sets the arrangement of UI windows for the video conference.
		/// </summary>
		/// <param name="mode"></param>
		public override void SetLayoutMode(eLayoutMode mode)
		{
			m_Video.SetLayout(eLayoutTarget.Local, GetLayoutFamily(mode));
		}

		#endregion

		#region Private Methods

		private void UpdateSelfView()
		{
			SelfViewEnabled = m_Video.SelfViewEnabled;
		}

		private void UpdateSelfViewFullscreen()
		{
			SelfViewFullScreenEnabled = m_Video.SelfViewFullScreenEnabled;
		}

		private void UpdateLayoutAvailable()
		{
			// We can only control layout when configured in single display mode
			LayoutAvailable = m_Video.Monitors == eMonitors.Single;
		}

		/// <summary>
		/// Gets the layout family for the given layout mode.
		/// </summary>
		/// <param name="mode"></param>
		/// <returns></returns>
		private static eLayoutFamily GetLayoutFamily(eLayoutMode mode)
		{
			switch (mode)
			{
				case eLayoutMode.Auto:
					return eLayoutFamily.Auto;
				case eLayoutMode.Custom:
					return eLayoutFamily.Custom;
				case eLayoutMode.Equal:
					return eLayoutFamily.Equal;
				case eLayoutMode.Overlay:
					return eLayoutFamily.Overlay;
				case eLayoutMode.Prominent:
					return eLayoutFamily.Prominent;
				case eLayoutMode.Single:
					return eLayoutFamily.Single;
				default:
					throw new ArgumentOutOfRangeException("mode");
			}
		}

		#endregion

		#region Video Component Callbacks

		/// <summary>
		/// Subscribe to the video component events.
		/// </summary>
		/// <param name="video"></param>
		private void Subscribe(VideoComponent video)
		{
			video.OnSelfViewEnabledChanged += VideoOnSelfViewEnabledChanged;
			video.OnSelfViewFullScreenEnabledChanged += VideoOnSelfViewFullScreenEnabledChanged;
			video.OnMonitorsChanged += VideoOnMonitorsChanged;
		}

		/// <summary>
		/// Unsubscribe from the video component events.
		/// </summary>
		/// <param name="video"></param>
		private void Unsubscribe(VideoComponent video)
		{
			video.OnSelfViewEnabledChanged -= VideoOnSelfViewEnabledChanged;
			video.OnSelfViewFullScreenEnabledChanged -= VideoOnSelfViewFullScreenEnabledChanged;
			video.OnMonitorsChanged -= VideoOnMonitorsChanged;
		}

		private void VideoOnSelfViewFullScreenEnabledChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateSelfViewFullscreen();
		}

		private void VideoOnSelfViewEnabledChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateSelfView();
		}

		private void VideoOnMonitorsChanged(object sender, MonitorsEventArgs monitorsEventArgs)
		{
			UpdateLayoutAvailable();
		}

		#endregion
	}
}
