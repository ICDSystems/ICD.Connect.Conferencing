using ICD.Connect.Conferencing.Controls.Presentation;

namespace ICD.Connect.Conferencing.Polycom.Devices.Controls
{
	public sealed class PolycomCodecPresentationControl : AbstractPresentationControl<PolycomGroupSeriesDevice>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecPresentationControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
		}

		#region Methods

		/// <summary>
		/// Starts presenting the source at the given input address.
		/// </summary>
		/// <param name="input"></param>
		public override void StartPresentation(int input)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Stops the active presentation.
		/// </summary>
		public override void StopPresentation()
		{
			throw new System.NotImplementedException();
		}

		#endregion
	}
}
