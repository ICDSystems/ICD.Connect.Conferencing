using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Controls.Presentation;
using ICD.Connect.Conferencing.Zoom.Components.Presentation;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public class ZoomRoomPresentationControl : AbstractPresentationControl<ZoomRoom>
	{
		private readonly PresentationComponent m_Component;

		public ZoomRoomPresentationControl(ZoomRoom parent, int id) : base(parent, id)
		{
			m_Component = Parent.Components.GetComponent<PresentationComponent>();
			Subscribe(m_Component);
		}

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_Component);
		}

		#region Methods

		/// <summary>
		/// Starts the presentation.
		/// </summary>
		/// <remarks>
		/// Zoom only supports one input, and has to be specifically Magewell or INOGENI.
		/// </remarks>
		/// <param name="input"></param>
		public override void StartPresentation(int input)
		{
			m_Component.StartPresentation();
		}

		/// <summary>
		/// Stops the presentation
		/// </summary>
		public override void StopPresentation()
		{
			m_Component.StopPresentation();
		}

		private void UpdatePresentationActive()
		{
			PresentationActive = m_Component != null && (m_Component.Sharing || m_Component.PresentationOutput != null);
		}

		#endregion

		#region Component Callbacks

		private void Subscribe(PresentationComponent component)
		{
			component.OnLocalSharingChanged += ComponentOnOnLocalSharingChanged;
			component.OnPresentationOutputChanged += ComponentOnPresentationOutputChanged;
		}

		private void Unsubscribe(PresentationComponent component)
		{
			component.OnLocalSharingChanged -= ComponentOnOnLocalSharingChanged;
		}

		private void ComponentOnOnLocalSharingChanged(object sender, BoolEventArgs args)
		{
			PresentationActiveInput = m_Component != null && m_Component.Sharing ? 1 : (int?) null;
			UpdatePresentationActive();
		}
		
		private void ComponentOnPresentationOutputChanged(object sender, PresentationOutputEventArgs e)
		{
			UpdatePresentationActive();
		}

		#endregion
	}
}