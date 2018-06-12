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
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Enables/disables the self-view fullscreen mode during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetSelfViewFullScreenEnabled(bool enabled)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Sets the arrangement of UI windows for the video conference.
		/// </summary>
		/// <param name="mode"></param>
		public override void SetLayoutMode(eLayoutMode mode)
		{
			throw new System.NotImplementedException();
		}

		#endregion

		#region Layout Callbacks

		private void Subscribe(LayoutComponent layoutComponent)
		{
			throw new System.NotImplementedException();
		}

		private void Unsubscribe(LayoutComponent layoutComponent)
		{
			throw new System.NotImplementedException();
		}

		#endregion
	}
}
