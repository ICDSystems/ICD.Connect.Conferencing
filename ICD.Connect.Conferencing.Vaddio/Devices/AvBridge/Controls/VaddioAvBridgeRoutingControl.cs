using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Controls.Routing;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Video;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Utils;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Controls
{
	public sealed class VaddioAvBridgeRoutingControl : AbstractVideoConferenceRouteControl<VaddioAvBridgeDevice>
	{
		#region Events

		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		#endregion

		private readonly SwitcherCache m_SwitcherCache;

		private readonly VaddioAvBridgeVideoComponent m_VideoComponent;

		private eVideoInput m_ActiveVideoInput;

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public VaddioAvBridgeRoutingControl(VaddioAvBridgeDevice parent, int id) 
			: base(parent, id)
		{
			m_SwitcherCache = new SwitcherCache();
			Subscribe(m_SwitcherCache);

			m_VideoComponent = parent.Components.GetComponent<VaddioAvBridgeVideoComponent>();
			Subscribe(m_VideoComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			OnSourceDetectionStateChange = null;
			OnActiveInputsChanged = null;
			OnActiveTransmissionStateChanged = null;

			Unsubscribe(m_SwitcherCache);
			Unsubscribe(m_VideoComponent);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Returns true if a signal is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return GetInputs().Any(i => i.Address == input && i.ConnectionType == type);
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
		/// Gets the input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override ConnectorInfo GetInput(int input)
		{
			return GetInputs().First(i => i.Address == input);
		}

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
		/// Returns true if the input is actively being used by the source device.
		/// For example, a display might true if the input is currently on screen,
		/// while a switcher may return true if the input is currently routed.
		/// </summary>
		public override bool GetInputActiveState(int input, eConnectionType type)
		{
			return input == GetInputAddressForActiveVideoInput();
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			// HDMI
			yield return new ConnectorInfo(1, eConnectionType.Video);
			// VGA (rgbhv OR ypbpr)
			yield return new ConnectorInfo(2, eConnectionType.Video);
			// Composite (sd)
			yield return new ConnectorInfo(3, eConnectionType.Video);
		}

		/// <summary>
		/// Sets the input address to use for the camera feed.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="cameraDeviceId"></param>
		public override void SetCameraInput(int address, int cameraDeviceId)
		{
			m_VideoComponent.SetVideoInput(GetVideoInputForInputAddress(address));
		}

		/// <summary>
		/// Gets the output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override ConnectorInfo GetOutput(int output)
		{
			return GetOutputs().First(o => o.Address == output);
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
			// USB 2.0 from bridge to PC
			yield return new ConnectorInfo(1, eConnectionType.Audio | eConnectionType.Video | eConnectionType.Usb);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Determines the hardcoded input address for the 
		/// current active video input from the component.
		/// </summary>
		/// <returns></returns>
		private int? GetInputAddressForActiveVideoInput()
		{
			switch (m_ActiveVideoInput)
			{
				case eVideoInput.None:
				case eVideoInput.auto:
					return null;
				case eVideoInput.hdmi:
					return 1;
				case eVideoInput.rgbhv:
				case eVideoInput.ypbpr:
					return 2;
				case eVideoInput.sd:
					return 3;

				default:
					throw new InvalidOperationException("Cannot determine input from active source");
			}
		}

		/// <summary>
		/// Determines the hardcoded Video Input type for the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		private eVideoInput GetVideoInputForInputAddress(int address)
		{
			switch (address)
			{
				case 1:
					return eVideoInput.hdmi;
				case 2:
					return eVideoInput.rgbhv;
				case 3:
					return eVideoInput.sd;

				default:
					return eVideoInput.None;
			}
		}

		#endregion

		#region SwitcherCache Callbacks

		private void Subscribe(SwitcherCache switcherCache)
		{
			switcherCache.OnSourceDetectionStateChange += SwitcherCacheOnSourceDetectionStateChange;
			switcherCache.OnActiveInputsChanged += SwitcherCacheOnActiveInputsChanged;
			switcherCache.OnActiveTransmissionStateChanged += SwitcherCacheOnActiveTransmissionStateChanged;
		}

		private void Unsubscribe(SwitcherCache switcherCache)
		{
			switcherCache.OnSourceDetectionStateChange -= SwitcherCacheOnSourceDetectionStateChange;
			switcherCache.OnActiveInputsChanged -= SwitcherCacheOnActiveInputsChanged;
			switcherCache.OnActiveTransmissionStateChanged -= SwitcherCacheOnActiveTransmissionStateChanged;
		}

		private void SwitcherCacheOnSourceDetectionStateChange(object sender, SourceDetectionStateChangeEventArgs e)
		{
			OnSourceDetectionStateChange.Raise(this, new SourceDetectionStateChangeEventArgs(e));
		}

		private void SwitcherCacheOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs e)
		{
			OnActiveInputsChanged.Raise(this, new ActiveInputStateChangeEventArgs(e));
		}

		private void SwitcherCacheOnActiveTransmissionStateChanged(object sender, TransmissionStateEventArgs e)
		{
			OnActiveTransmissionStateChanged.Raise(this, new TransmissionStateEventArgs(e));
		}

		#endregion

		#region Video Component Callbacks

		private void Subscribe(VaddioAvBridgeVideoComponent videoComponent)
		{
			videoComponent.OnVideoInputChanged += VideoComponentOnVideoInputChanged;
		}

		private void Unsubscribe(VaddioAvBridgeVideoComponent videoComponent)
		{
			videoComponent.OnVideoInputChanged += VideoComponentOnVideoInputChanged;
		}

		private void VideoComponentOnVideoInputChanged(object sender, GenericEventArgs<eVideoInput> genericEventArgs)
		{
			eVideoInput input = genericEventArgs.Data;

			m_ActiveVideoInput = input;

			m_SwitcherCache.SetInputForOutput(1, GetInputAddressForActiveVideoInput(),
			                                  eConnectionType.Audio | eConnectionType.Video | eConnectionType.Usb);
		}

		#endregion
	}
}