using ICD.Connect.Conferencing.Controls.Presentation;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Controls
{
	public sealed class VaddioAvBridgePresentationControl : AbstractPresentationControl<VaddioAvBridgeDevice>
	{
		public VaddioAvBridgePresentationControl(VaddioAvBridgeDevice parent, int id) 
			: base(parent, id)
		{
		}

		public override void StartPresentation(int input)
		{
			PresentationActive = true;
			PresentationActiveInput = input;
		}

		public override void StopPresentation()
		{
			PresentationActive = false;
			PresentationActiveInput = null;
		}
	}
}
