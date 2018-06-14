using System;
using ICD.Connect.Conferencing.Controls.Layout;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Layout;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls
{
	public sealed class PolycomCodecLayoutControl : AbstractConferenceLayoutControl<PolycomGroupSeriesDevice>
	{
		private readonly LayoutComponent m_LayoutComponent;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecLayoutControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
			m_LayoutComponent = parent.Components.GetComponent<LayoutComponent>();

			Subscribe(m_LayoutComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_LayoutComponent);
		}

		#region Methods

		/// <summary>
		/// Enables/disables the self-view window during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetSelfViewEnabled(bool enabled)
		{
			m_LayoutComponent.SetSelfView(enabled ? eSelfView.On : eSelfView.Off);
		}

		/// <summary>
		/// Enables/disables the self-view fullscreen mode during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetSelfViewFullScreenEnabled(bool enabled)
		{
		}

		/// <summary>
		/// Sets the arrangement of UI windows for the video conference.
		/// </summary>
		/// <param name="mode"></param>
		public override void SetLayoutMode(eLayoutMode mode)
		{
		}

		#endregion

		#region Layout Callbacks

		/// <summary>
		/// Subscribe to the layout component events.
		/// </summary>
		/// <param name="layoutComponent"></param>
		private void Subscribe(LayoutComponent layoutComponent)
		{
			layoutComponent.OnSelfViewChanged += LayoutComponentOnSelfViewChanged;
		}

		/// <summary>
		/// Unsubscribe from the layout component events.
		/// </summary>
		/// <param name="layoutComponent"></param>
		private void Unsubscribe(LayoutComponent layoutComponent)
		{
			layoutComponent.OnSelfViewChanged += LayoutComponentOnSelfViewChanged;
		}

		/// <summary>
		/// Called when the selfview mode changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="selfViewEventArgs"></param>
		private void LayoutComponentOnSelfViewChanged(object sender, SelfViewEventArgs selfViewEventArgs)
		{
			eSelfView selfView = m_LayoutComponent.SelfView;

			// Assume "auto" is "enabled"
			SelfViewEnabled = selfView != eSelfView.Off;
		}

		#endregion
	}
}
