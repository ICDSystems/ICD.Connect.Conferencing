using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Services;
using ICD.Connect.Conferencing.Controls.Routing;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Camera;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Content;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Controls
{
	public sealed class PolycomCodecRoutingControl : AbstractVideoConferenceRouteControl<PolycomGroupSeriesDevice>
	{
		/// <summary>
		/// Raised when an input source status changes.
		/// </summary>
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;

		/// <summary>
		/// Raised when the device starts/stops actively using an input, e.g. unroutes an input.
		/// </summary>
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;

		/// <summary>
		/// Raised when the device starts/stops actively transmitting on an output.
		/// </summary>
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		private readonly CameraComponent m_CameraComponent;
		private readonly ContentComponent m_ContentComponent;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecRoutingControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
			m_CameraComponent = parent.Components.GetComponent<CameraComponent>();
			m_ContentComponent = parent.Components.GetComponent<ContentComponent>();

			Subscribe(m_CameraComponent);
			Subscribe(m_ContentComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnSourceDetectionStateChange = null;
			OnActiveInputsChanged = null;
			OnActiveTransmissionStateChanged = null;

			base.DisposeFinal(disposing);

			Unsubscribe(m_CameraComponent);
			Unsubscribe(m_ContentComponent);
		}

		#region Methods

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return
				ServiceProvider.GetService<IRoutingGraph>()
				               .Connections
				               .GetChildren()
				               .Where(c => c.Destination.Device == Parent.Id && c.Destination.Control == Id)
				               .Select(c => new ConnectorInfo(c.Destination.Address, c.ConnectionType));
		}

		/// <summary>
		/// Returns the true if the input is actively being used by the source device.
		/// For example, a display might true if the input is currently on screen,
		/// while a switcher may return true if the input is currently routed.
		/// </summary>
		public override bool GetInputActiveState(int input, eConnectionType type)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns true if a signal is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets the input address to use for the camera feed.
		/// </summary>
		/// <param name="address"></param>
		public override void SetCameraInput(int address)
		{
			m_CameraComponent.SetNearCameraAsVideoSource(address);
		}

		/// <summary>
		/// Returns the outputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			return
				ServiceProvider.GetService<IRoutingGraph>()
				               .Connections
				               .GetChildren()
				               .Where(c => c.Source.Device == Parent.Id && c.Source.Control == Id)
				               .Select(c => new ConnectorInfo(c.Source.Address, c.ConnectionType));
		}

		/// <summary>
		/// Returns true if the device is actively transmitting on the given output.
		/// This is NOT the same as sending video, since some devices may send an
		/// idle signal by default.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetActiveTransmissionState(int output, eConnectionType type)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region CameraComponent Callbacks

		/// <summary>
		/// Subscribe to the camera component events.
		/// </summary>
		/// <param name="cameraComponent"></param>
		private void Subscribe(CameraComponent cameraComponent)
		{
			cameraComponent.OnActiveNearCameraChanged += CameraComponentOnActiveNearCameraChanged;
		}

		/// <summary>
		/// Unsubscribe from the camera component events.
		/// </summary>
		/// <param name="cameraComponent"></param>
		private void Unsubscribe(CameraComponent cameraComponent)
		{
			cameraComponent.OnActiveNearCameraChanged -= CameraComponentOnActiveNearCameraChanged;
		}

		/// <summary>
		/// Called when the active camera changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CameraComponentOnActiveNearCameraChanged(object sender, ActiveCameraEventArgs eventArgs)
		{
		}

		#endregion

		#region ContentComponent Callbacks

		/// <summary>
		/// Subscribe to the content component events.
		/// </summary>
		/// <param name="contentComponent"></param>
		private void Subscribe(ContentComponent contentComponent)
		{
			contentComponent.OnContentVideoSourceChanged += ContentComponentOnContentVideoSourceChanged;
		}

		/// <summary>
		/// Unsubscribe from the content component events.
		/// </summary>
		/// <param name="contentComponent"></param>
		private void Unsubscribe(ContentComponent contentComponent)
		{
			contentComponent.OnContentVideoSourceChanged -= ContentComponentOnContentVideoSourceChanged;
		}

		/// <summary>
		/// Called when the presentation video source changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ContentComponentOnContentVideoSourceChanged(object sender, ContentVideoSourceEventArgs eventArgs)
		{
		}

		#endregion
	}
}
