using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Controls.Routing;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Audio;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Video;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Utils;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Controls
{
	public sealed class VaddioAvBridgeRoutingControl : AbstractVideoConferenceRouteControl<VaddioAvBridgeDevice>
	{
		private const int OUTPUT_ADDRESS = 1;

		private const int INPUT_ADDRESS_HDMI = 1;
		private const int INPUT_ADDRESS_VGA = 2;
		private const int INPUT_ADDRESS_COMPOSITE = 3;

		private const int INPUT_ADDRESS_BALANCED = 1;
		private const int INPUT_ADDRESS_UNBALANCED = 2;

		#region Events

		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		#endregion

		private readonly SwitcherCache m_SwitcherCache;
		private readonly VaddioAvBridgeVideoComponent m_VideoComponent;
		private readonly VaddioAvBridgeAudioComponent m_AudioComponent;

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

			m_AudioComponent = parent.Components.GetComponent<VaddioAvBridgeAudioComponent>();
			Subscribe(m_AudioComponent);
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
			Unsubscribe(m_VideoComponent);
			Unsubscribe(m_AudioComponent);
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
			return GetInputs().Any(i => i.Address == input && i.ConnectionType.HasFlags(type));
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
			return m_SwitcherCache.GetInputForOutput(OUTPUT_ADDRESS, type) != null;
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			IEnumerable<int> videoAddresses =
				EnumUtils.GetValues<eVideoInput>()
				         .Select(input => VideoInputToAddress(input))
				         .ExceptNulls();

			foreach (int videoAddress in videoAddresses)
				yield return new ConnectorInfo(videoAddress, eConnectionType.Video);

			IEnumerable<int> audioAddresses =
				EnumUtils.GetValues<eAudioInput>()
				         .Select(input => AudioInputToAddress(input))
				         .ExceptNulls();

			foreach (int audioAddress in audioAddresses)
				yield return new ConnectorInfo(audioAddress, eConnectionType.Audio);
		}

		/// <summary>
		/// Sets the input address to use for the camera feed.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="cameraDeviceId"></param>
		public override void SetCameraInput(int address, int cameraDeviceId)
		{
			m_VideoComponent.SetVideoInput(AddressToVideoInput(address));
		}

		// TODO - SetContentInput

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
			yield return new ConnectorInfo(OUTPUT_ADDRESS, eConnectionType.Audio | eConnectionType.Video);
		}

		#endregion

		#region Private Methods


		private static int? VideoInputToAddress(eVideoInput input)
		{
			switch (input)
			{
				case eVideoInput.None:
				case eVideoInput.auto:
					return null;
				case eVideoInput.hdmi:
					return INPUT_ADDRESS_HDMI;
				case eVideoInput.rgbhv:
				case eVideoInput.ypbpr:
					return INPUT_ADDRESS_VGA;
				case eVideoInput.sd:
					return INPUT_ADDRESS_COMPOSITE;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private static int? AudioInputToAddress(eAudioInput input)
		{
			switch (input)
			{
				case eAudioInput.None:
					return null;
				case eAudioInput.balanced:
					return INPUT_ADDRESS_BALANCED;
				case eAudioInput.unbalanced:
					return INPUT_ADDRESS_UNBALANCED;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Determines the hardcoded Video Input type for the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		private static eVideoInput AddressToVideoInput(int address)
		{
			switch (address)
			{
				case INPUT_ADDRESS_HDMI:
					return eVideoInput.hdmi;
				case INPUT_ADDRESS_VGA:
					return eVideoInput.rgbhv;
				case INPUT_ADDRESS_COMPOSITE:
					return eVideoInput.sd;

				default:
					return eVideoInput.None;
			}
		}

		private static eAudioInput AddressToAudioInput(int address)
		{
			switch (address)
			{
				case INPUT_ADDRESS_BALANCED:
					return eAudioInput.balanced;
				case INPUT_ADDRESS_UNBALANCED:
					return eAudioInput.unbalanced;

				default:
					return eAudioInput.None;
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
			videoComponent.OnVideoInputChanged -= VideoComponentOnVideoInputChanged;
		}

		private void VideoComponentOnVideoInputChanged(object sender, GenericEventArgs<eVideoInput> e)
		{
			int? address = VideoInputToAddress(e.Data);
			m_SwitcherCache.SetInputForOutput(OUTPUT_ADDRESS, address, eConnectionType.Video);
		}

		#endregion

		#region Audio Component Callbacks

		private void Subscribe(VaddioAvBridgeAudioComponent audioComponent)
		{
			audioComponent.OnAudioInputChanged += AudioComponentOnOnAudioInputChanged;
		}

		private void Unsubscribe(VaddioAvBridgeAudioComponent audioComponent)
		{
			audioComponent.OnAudioInputChanged -= AudioComponentOnOnAudioInputChanged;
		}

		private void AudioComponentOnOnAudioInputChanged(object sender, GenericEventArgs<eAudioInput> e)
		{
			int? address = AudioInputToAddress(e.Data);
			m_SwitcherCache.SetInputForOutput(OUTPUT_ADDRESS, address, eConnectionType.Audio);
		}

		#endregion
	}
}