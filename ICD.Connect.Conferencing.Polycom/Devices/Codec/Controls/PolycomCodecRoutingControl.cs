using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Connect.Conferencing.Controls.Routing;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Camera;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Content;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Routing.Utils;

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

		private readonly SwitcherCache m_SwitcherCache;

		private readonly CameraComponent m_CameraComponent;
		private readonly ContentComponent m_ContentComponent;

		private IRoutingGraph m_CachedRoutingGraph;

		/// <summary>
		/// Gets the routing graph.
		/// </summary>
		public IRoutingGraph RoutingGraph
		{
			get { return m_CachedRoutingGraph = m_CachedRoutingGraph ?? ServiceProvider.GetService<IRoutingGraph>(); }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public PolycomCodecRoutingControl(PolycomGroupSeriesDevice parent, int id)
			: base(parent, id)
		{
			m_SwitcherCache = new SwitcherCache();
			Subscribe(m_SwitcherCache);

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

			Unsubscribe(m_SwitcherCache);

			Unsubscribe(m_CameraComponent);
			Unsubscribe(m_ContentComponent);
		}

		#region Methods

		/// <summary>
		/// Returns true if the destination contains an input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override bool ContainsInput(int input)
		{
			return RoutingGraph.Connections.GetInputConnection(this, input) != null;
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return
				RoutingGraph.Connections
							.GetInputConnections(Parent.Id, Id)
							.Select(c => new ConnectorInfo(c.Destination.Address, c.ConnectionType));
		}

		/// <summary>
		/// Returns the true if the input is actively being used by the source device.
		/// For example, a display might true if the input is currently on screen,
		/// while a switcher may return true if the input is currently routed.
		/// </summary>
		public override bool GetInputActiveState(int input, eConnectionType type)
		{
			return true;
		}

		/// <summary>
		/// Gets the input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override ConnectorInfo GetInput(int input)
		{
			Connection connection = RoutingGraph.Connections.GetInputConnection(this, input);
			if (connection == null)
				throw new ArgumentOutOfRangeException("input");

			return new ConnectorInfo(connection.Destination.Address, connection.ConnectionType);
		}

		/// <summary>
		/// Returns true if a signal is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return true;
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
		/// Returns true if the source contains an output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override bool ContainsOutput(int output)
		{
			return RoutingGraph.Connections.GetOutputConnection(this, output) != null;
		}

		/// <summary>
		/// Returns the outputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			return
				RoutingGraph.Connections
							.GetOutputConnections(Parent.Id, Id)
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
			return true;
		}

		/// <summary>
		/// Gets the output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override ConnectorInfo GetOutput(int output)
		{
			Connection connection = RoutingGraph.Connections.GetOutputConnection(this, output);
			if (connection == null)
				throw new ArgumentOutOfRangeException("output");

			return new ConnectorInfo(connection.Source.Address, connection.ConnectionType);
		}

		#endregion

		#region SwitcherCache Callbacks

		/// <summary>
		/// Subscribe to the switcher cache events.
		/// </summary>
		/// <param name="switcherCache"></param>
		private void Subscribe(SwitcherCache switcherCache)
		{
			switcherCache.OnSourceDetectionStateChange += SwitcherCacheOnSourceDetectionStateChange;
			switcherCache.OnActiveInputsChanged += SwitcherCacheOnActiveInputsChanged;
			switcherCache.OnActiveTransmissionStateChanged += SwitcherCacheOnActiveTransmissionStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the switcher cache events.
		/// </summary>
		/// <param name="switcherCache"></param>
		private void Unsubscribe(SwitcherCache switcherCache)
		{
			switcherCache.OnSourceDetectionStateChange -= SwitcherCacheOnSourceDetectionStateChange;
			switcherCache.OnActiveInputsChanged -= SwitcherCacheOnActiveInputsChanged;
			switcherCache.OnActiveTransmissionStateChanged -= SwitcherCacheOnActiveTransmissionStateChanged;
		}

		private void SwitcherCacheOnActiveTransmissionStateChanged(object sender, TransmissionStateEventArgs eventArgs)
		{
			OnActiveTransmissionStateChanged.Raise(this, new TransmissionStateEventArgs(eventArgs));
		}

		private void SwitcherCacheOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs eventArgs)
		{
			OnActiveInputsChanged.Raise(this, new ActiveInputStateChangeEventArgs(eventArgs));
		}

		private void SwitcherCacheOnSourceDetectionStateChange(object sender, SourceDetectionStateChangeEventArgs eventArgs)
		{
			OnSourceDetectionStateChange.Raise(this, new SourceDetectionStateChangeEventArgs(eventArgs));
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
