using ICD.Connect.Conferencing.Controls.Layout;

namespace ICD.Connect.Conferencing.Polycom.Devices.Controls
{
	public sealed class PolycomCodecLayoutControl : AbstractConferenceLayoutControl<PolycomGroupSeriesDevice>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecLayoutControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
		}

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
	}
}
