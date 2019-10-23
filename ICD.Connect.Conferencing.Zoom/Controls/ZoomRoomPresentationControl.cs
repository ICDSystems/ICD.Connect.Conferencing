using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Controls.Presentation;
using ICD.Connect.Conferencing.Zoom.Components.Layout;
using ICD.Connect.Conferencing.Zoom.Components.Presentation;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomPresentationControl : AbstractPresentationControl<ZoomRoom>
	{
		[NotNull]
		private readonly PresentationComponent m_PresentationComponent;
		private readonly LayoutComponent m_LayoutComponent;


		public ZoomRoomPresentationControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			m_PresentationComponent = Parent.Components.GetComponent<PresentationComponent>();
			m_LayoutComponent = Parent.Components.GetComponent<LayoutComponent>();
			Subscribe(m_PresentationComponent);
			Subscribe(m_LayoutComponent);
		}

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_PresentationComponent);
			Unsubscribe(m_LayoutComponent);
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
			m_PresentationComponent.StartPresentation();
		}

		/// <summary>
		/// Stops the presentation
		/// </summary>
		public override void StopPresentation()
		{
			m_PresentationComponent.StopPresentation();
		}

		private void UpdatePresentationActive()
		{
			PresentationActive = m_PresentationComponent.Sharing ||
			                     m_LayoutComponent.ShareThumb ||
			                     m_PresentationComponent.PresentationOutput != null;
		}

		#endregion

		#region Presentation Component Callbacks

		private void Subscribe(PresentationComponent component)
		{
			component.OnLocalSharingChanged += ComponentOnLocalSharingChanged;
			component.OnPresentationOutputChanged += ComponentOnPresentationOutputChanged;
		}

		private void Unsubscribe(PresentationComponent component)
		{
			component.OnLocalSharingChanged -= ComponentOnLocalSharingChanged;
			component.OnPresentationOutputChanged -= ComponentOnPresentationOutputChanged;
		}

		private void ComponentOnLocalSharingChanged(object sender, BoolEventArgs args)
		{
			PresentationActiveInput = m_PresentationComponent.Sharing ? 1 : (int?) null;
			UpdatePresentationActive();
		}
		
		private void ComponentOnPresentationOutputChanged(object sender, PresentationOutputEventArgs e)
		{
			UpdatePresentationActive();
		}

		#endregion

		#region Layout Component Callbacks

		private void Subscribe(LayoutComponent component)
		{
			component.OnShareThumbChanged += LayoutComponentOnShareThumbLayoutChanged;
		}

		private void Unsubscribe(LayoutComponent component)
		{
			component.OnShareThumbChanged -= LayoutComponentOnShareThumbLayoutChanged;
		}

		private void LayoutComponentOnShareThumbLayoutChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdatePresentationActive();
		}
		#endregion
	}
}