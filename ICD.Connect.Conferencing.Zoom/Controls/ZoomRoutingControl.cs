using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Connect.Conferencing.Controls.Routing;
using ICD.Connect.Conferencing.Zoom.Components.Camera;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Routing.Utils;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public class ZoomRoutingControl : AbstractVideoConferenceRouteControl<ZoomRoom>
	{
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		private readonly SwitcherCache m_SwitcherCache;

		private readonly CameraComponent m_CameraComponent;

		public ZoomRoutingControl(ZoomRoom parent, int id) : base(parent, id)
		{
			m_SwitcherCache = new SwitcherCache();
			Subscribe(m_SwitcherCache);

			m_CameraComponent = parent.Components.GetComponent<CameraComponent>();

			Subscribe(m_CameraComponent);
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
			return true;
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
			return true;
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
	}
}