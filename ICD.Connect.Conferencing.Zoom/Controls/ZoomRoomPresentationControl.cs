using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Timers;
using ICD.Connect.Conferencing.Controls.Presentation;
using ICD.Connect.Conferencing.Zoom.Components.Layout;
using ICD.Connect.Conferencing.Zoom.Components.Presentation;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomPresentationControl : AbstractPresentationControl<ZoomRoom>
	{
		private const long STOP_SHARING_DEBOUNCE_TIME = 5 * 1000;

		[NotNull]
		private readonly PresentationComponent m_PresentationComponent;

		[NotNull]
		private readonly LayoutComponent m_LayoutComponent;

		private readonly SafeTimer m_StopSharingDebounceTimer;
		private bool m_RequestedSharing;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ZoomRoomPresentationControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			m_PresentationComponent = Parent.Components.GetComponent<PresentationComponent>();
			m_LayoutComponent = Parent.Components.GetComponent<LayoutComponent>();

			m_StopSharingDebounceTimer = SafeTimer.Stopped(SharingDebounceTimeout);

			Subscribe(m_PresentationComponent);
			Subscribe(m_LayoutComponent);
		}

		protected override void DisposeFinal(bool disposing)
		{
			m_StopSharingDebounceTimer.Dispose();

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
			m_RequestedSharing = true;
			m_PresentationComponent.StartPresentation();

			m_StopSharingDebounceTimer.Reset(STOP_SHARING_DEBOUNCE_TIME);
		}

		/// <summary>
		/// Stops the presentation
		/// </summary>
		public override void StopPresentation()
		{
			m_RequestedSharing = false;
			m_PresentationComponent.StopPresentation();
		}

		#endregion

		#region Private Methods

		private void SharingDebounceTimeout()
		{
			UpdatePresentationActive();
		}

		private void UpdatePresentationActive()
		{
			bool sharing = m_PresentationComponent.Sharing ||
			               m_LayoutComponent.ShareThumb ||
			               m_PresentationComponent.PresentationOutput != null;

			// Force Zoom to match the requested share state
			if (m_RequestedSharing && !sharing)
				m_PresentationComponent.StartPresentation();

			bool sharingFeedback = sharing || m_RequestedSharing;

			PresentationActive = sharingFeedback;
			PresentationActiveInput = sharingFeedback ? 1 : (int?)null;
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
