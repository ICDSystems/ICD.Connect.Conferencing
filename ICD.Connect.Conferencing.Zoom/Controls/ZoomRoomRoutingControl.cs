using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Controls.Routing;
using ICD.Connect.Conferencing.Zoom.Components.Camera;
using ICD.Connect.Conferencing.Zoom.Components.Presentation;
using ICD.Connect.Devices;
using ICD.Connect.Devices.Windows;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;
using ICD.Connect.Routing.Utils;
using ICD.Connect.Settings.Cores;
using ICD.Connect.Settings.Originators;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	/// <summary>
	/// Routing control for the Zoom Room.
	/// </summary>
	/// <remarks>
	/// Input address 1 is reserved for the officially supported HDMI->USB dongles. Input addresses 2+ are for USB camera inputs.
	/// </remarks>
	public sealed class ZoomRoomRoutingControl : AbstractVideoConferenceRouteControl<ZoomRoom>
	{
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		private readonly SwitcherCache m_SwitcherCache;

		private readonly CameraComponent m_CameraComponent;
		private readonly PresentationComponent m_PresentationComponent;

		private IRoutingGraph m_CachedRoutingGraph;
		private string m_LastSelectedCameraUsbId;

		/// <summary>
		/// Gets the routing graph.
		/// </summary>
		private IRoutingGraph RoutingGraph
		{
			get { return m_CachedRoutingGraph = m_CachedRoutingGraph ?? ServiceProvider.GetService<IRoutingGraph>(); }
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ZoomRoomRoutingControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			m_SwitcherCache = new SwitcherCache();
			Subscribe(m_SwitcherCache);

			m_CameraComponent = parent.Components.GetComponent<CameraComponent>();
			m_PresentationComponent = parent.Components.GetComponent<PresentationComponent>();

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
		/// Returns true if the destination contains an input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override bool ContainsInput(int input)
		{
			return GetInputs().Any(i => i.Address == input);
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
			if (input == 1)
				return m_PresentationComponent != null &&
				       m_PresentationComponent.InputConnected &&
				       m_PresentationComponent.SharingState == eSharingState.Sending;

			return true;
		}

		/// <summary>
		/// Gets the input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override ConnectorInfo GetInput(int input)
		{
			return GetInputs().Single(i => i.Address == input);
		}

		/// <summary>
		/// Returns true if a signal is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			if (input == 1)
				return m_PresentationComponent != null && m_PresentationComponent.InputConnected && m_PresentationComponent.SignalDetected;
			return true;
		}

		/// <summary>
		/// Sets the input address to use for the camera feed.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="cameraDeviceId"></param>
		public override void SetCameraInput(int address, int cameraDeviceId)
		{
			IOriginator camera;
			if (!Parent.Core.Originators.TryGetChild(cameraDeviceId, out camera))
			{
				Logger.Log(eSeverity.Error, "Failed to find device for camera ID {0}", cameraDeviceId);
				return;
			}

			string zoomUsbId = GetZoomUsbIdForOriginator(camera);
			if (string.IsNullOrEmpty(zoomUsbId))
			{
				Logger.Log(eSeverity.Error, "Failed to find USB ID for {0}", camera);
				return;
			}

			m_LastSelectedCameraUsbId = zoomUsbId;

			m_CameraComponent.SetActiveCameraByUsbId(zoomUsbId);
		}

		/// <summary>
		/// Returns true if the source contains an output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override bool ContainsOutput(int output)
		{
			return GetOutputs().Any(o => o.Address == output);
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

		#region Private Methods

		[CanBeNull]
		private string GetDefaultCameraUsbId()
		{
			return Parent.DefaultCamera == null ? null : GetZoomUsbIdForOriginator(Parent.DefaultCamera);
		}

		[CanBeNull]
		private string GetZoomUsbIdForOriginator([NotNull] IOriginator camera)
		{
			// Is the camera part of the USB table?
			IDeviceBase device = camera as IDeviceBase;
			if (device != null && Parent.GetUsbIdForCamera(device).HasValue)
			{
				string usbId = GetBestUsbId(Parent.GetUsbIdForCamera(device));
				if (usbId != null)
					Parent.SetUsbIdForCamera(device, new WindowsDevicePathInfo(usbId));
				return usbId;
			}

			// Does the camera give us USB information?
			IWindowsDevice windowsCamera = camera as IWindowsDevice;
			if (windowsCamera != null)
				return GetBestUsbId(windowsCamera.DevicePath);

			return null;
		}

		[CanBeNull]
		private string GetBestUsbId(WindowsDevicePathInfo? windowsDeviceInfo)
		{
			if (windowsDeviceInfo == null)
				return null;

			// First try to find the USB ID that matches perfectly
			string output;
			bool found =
				m_CameraComponent.GetCameras()
				                 .Select(c => c.UsbId)
				                 .TryFirst(u => new WindowsDevicePathInfo(u) == windowsDeviceInfo, out output);
			if (found)
				return output;

			// Now try matching on the DeviceID portion
			return
				m_CameraComponent.GetCameras()
				                 .Select(c => c.UsbId)
				                 .FirstOrDefault(u => string.Equals(new WindowsDevicePathInfo(u).DeviceId,
				                                                    windowsDeviceInfo.Value.DeviceId,
				                                                    StringComparison.OrdinalIgnoreCase));
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

		#region Camera Component

		private void Subscribe(CameraComponent cameraComponent)
		{
			cameraComponent.OnCamerasUpdated += CameraComponentOnCamerasUpdated;
			cameraComponent.OnActiveCameraUpdated += CameraComponentOnActiveCameraUpdated;
		}

		private void Unsubscribe(CameraComponent cameraComponent)
		{
			cameraComponent.OnCamerasUpdated -= CameraComponentOnCamerasUpdated;
			cameraComponent.OnActiveCameraUpdated -= CameraComponentOnActiveCameraUpdated;
		}

		private void CameraComponentOnActiveCameraUpdated(object sender, EventArgs e)
		{
			UpdateActiveActiveCamera();
		}

		private void CameraComponentOnCamerasUpdated(object sender, EventArgs e)
		{
			UpdateActiveActiveCamera();
		}

		/// <summary>
		/// Ensure we are always setting either the previously selected camera or the default
		/// camera when available.
		/// </summary>
		private void UpdateActiveActiveCamera()
		{
			string activeCameraUsbId =
				m_CameraComponent.ActiveCamera == null
					? null
					: m_CameraComponent.ActiveCamera.UsbId;

			List<string> bestCameraUsbIds = new List<string>();

			// If a camera has been selected put that at the front of the list
			if (!string.IsNullOrEmpty(m_LastSelectedCameraUsbId))
				bestCameraUsbIds.Add(m_LastSelectedCameraUsbId);

			// Add the default camera to the end of the list
			string defaultCameraUsbId = GetDefaultCameraUsbId();
			if (!string.IsNullOrEmpty(defaultCameraUsbId))
				bestCameraUsbIds.Add(defaultCameraUsbId);

			// Make sure the best available camera is selected
			foreach (string usbId in bestCameraUsbIds)
			{
				string usbIdClosure = usbId;
				bool connected = m_CameraComponent.GetCameras().Select(c => c.UsbId).Any(u => u == usbIdClosure);
				if (!connected)
					continue;

				// Is the camera already active?
				if (usbId == activeCameraUsbId)
					return;

				// Set this camera as active
				m_CameraComponent.SetActiveCameraByUsbId(usbId);

				return;
			}
		}

		#endregion
	}
}