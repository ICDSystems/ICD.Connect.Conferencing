using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Timers;
using ICD.Connect.Conferencing.Controls.Presentation;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Call;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Layout;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Controls
{
	public sealed class ZoomRoomPresentationControl : AbstractPresentationControl<ZoomRoom>
	{
		private const long STOP_SHARING_DEBOUNCE_TIME = 2 * 1000;

		[NotNull] private readonly PresentationComponent m_PresentationComponent;
		[NotNull] private readonly LayoutComponent m_LayoutComponent;
		[NotNull] private readonly CallComponent m_CallComponent;
		[NotNull] private readonly SafeTimer m_StopSharingDebounceTimer;

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
			m_CallComponent = Parent.Components.GetComponent<CallComponent>();

			m_StopSharingDebounceTimer = SafeTimer.Stopped(SharingDebounceTimeout);

			Subscribe(m_PresentationComponent);
			Subscribe(m_LayoutComponent);
			Subscribe(m_CallComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			m_StopSharingDebounceTimer.Dispose();

			base.DisposeFinal(disposing);

			Unsubscribe(m_PresentationComponent);
			Unsubscribe(m_LayoutComponent);
			Unsubscribe(m_CallComponent);
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
			if (m_RequestedSharing && m_PresentationComponent.SharingState == eSharingState.None)
				m_PresentationComponent.StartPresentation();
		}

		/// <summary>
		/// Updates the feedback for the current presentation state.
		/// </summary>
		private void UpdatePresentationActive()
		{
			switch (m_PresentationComponent.SharingState)
			{
				case eSharingState.None:
					// Zoom likes to randomly drop the presentation sometimes,
					// so massage the feedback and restart the presentation
					PresentationActive = m_RequestedSharing;
					PresentationActiveInput = m_RequestedSharing ? 1 : (int?)null;
					if (m_RequestedSharing)
						m_StopSharingDebounceTimer.Reset(STOP_SHARING_DEBOUNCE_TIME);
					break;

				case eSharingState.Sending:
					// The presentation is sending
					PresentationActive = true;
					PresentationActiveInput = 1;
					break;

				case eSharingState.Receiving:
					// Clear the requested sending state and the feedback
					m_RequestedSharing = false;
					PresentationActive = false;
					PresentationActiveInput = null;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		#region Presentation Component Callbacks

		/// <summary>
		/// Subscribe to the presentation component events.
		/// </summary>
		/// <param name="component"></param>
		private void Subscribe(PresentationComponent component)
		{
			component.OnSharingStateChanged += ComponentOnSharingStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the presentation component events.
		/// </summary>
		/// <param name="component"></param>
		private void Unsubscribe(PresentationComponent component)
		{
			component.OnSharingStateChanged -= ComponentOnSharingStateChanged;
		}

		/// <summary>
		/// Called when the sharing state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ComponentOnSharingStateChanged(object sender, GenericEventArgs<eSharingState> eventArgs)
		{
			UpdatePresentationActive();
		}

		#endregion

		#region Layout Component Callbacks

		/// <summary>
		/// Subscribe to the layout component events.
		/// </summary>
		/// <param name="component"></param>
		private void Subscribe(LayoutComponent component)
		{
			component.OnShareThumbChanged += LayoutComponentOnShareThumbLayoutChanged;
		}

		/// <summary>
		/// Unsubscribe from the layout component events.
		/// </summary>
		/// <param name="component"></param>
		private void Unsubscribe(LayoutComponent component)
		{
			component.OnShareThumbChanged -= LayoutComponentOnShareThumbLayoutChanged;
		}

		/// <summary>
		/// Called when the sharing thumbnail layout changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void LayoutComponentOnShareThumbLayoutChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdatePresentationActive();
		}

		#endregion

		#region Call Component Callbacks

		/// <summary>
		/// Subscribe to the call component events.
		/// </summary>
		/// <param name="component"></param>
		private void Subscribe(CallComponent component)
		{
			component.OnStatusChanged += CallComponentOnStatusChanged;
		}

		/// <summary>
		/// Unsubscribe from the call component events.
		/// </summary>
		/// <param name="component"></param>
		private void Unsubscribe(CallComponent component)
		{
			component.OnStatusChanged -= CallComponentOnStatusChanged;
		}

		/// <summary>
		/// Called when the call component status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnStatusChanged(object sender, GenericEventArgs<eCallStatus> eventArgs)
		{
			// Clear the requested presentation state when no-longer in a meeting
			if (m_CallComponent.Status != eCallStatus.IN_MEETING)
				m_RequestedSharing = false;
		}

		#endregion
	}
}
