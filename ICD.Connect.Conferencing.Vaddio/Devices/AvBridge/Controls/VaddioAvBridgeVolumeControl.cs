using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Audio;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Controls
{
	public sealed class VaddioAvBridgeVolumeControl : AbstractVolumeDeviceControl<VaddioAvBridgeDevice>
	{
		private readonly VaddioAvBridgeAudioComponent m_AudioComponent;

		#region Properties

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public override float VolumeLevelMin { get { return 0; } }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public override float VolumeLevelMax { get { return 10; } }

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public VaddioAvBridgeVolumeControl(VaddioAvBridgeDevice parent, int id)
			: base(parent, id)
		{
			m_AudioComponent = parent.Components.GetComponent<VaddioAvBridgeAudioComponent>();

			SupportedVolumeFeatures = eVolumeFeatures.Mute |
			                          eVolumeFeatures.MuteAssignment |
			                          eVolumeFeatures.MuteFeedback |
			                          eVolumeFeatures.Volume |
			                          eVolumeFeatures.VolumeAssignment |
			                          eVolumeFeatures.VolumeFeedback;

			Subscribe(m_AudioComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_AudioComponent);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			m_AudioComponent.SetAudioMute(mute);
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public override void ToggleIsMuted()
		{
			m_AudioComponent.ToggleAudioMute();
		}

		/// <summary>
		/// Sets the raw volume level in the device volume representation.
		/// </summary>
		/// <param name="level"></param>
		public override void SetVolumeLevel(float level)
		{
			m_AudioComponent.SetAudioVolume((int)level);
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			m_AudioComponent.IncrementAudioVolume();
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			m_AudioComponent.DecrementAudioVolume();
		}

		/// <summary>
		/// Starts ramping the volume, and continues until stop is called or the timeout is reached.
		/// If already ramping the current timeout is updated to the new timeout duration.
		/// </summary>
		/// <param name="increment">Increments the volume if true, otherwise decrements.</param>
		/// <param name="timeout"></param>
		public override void VolumeRamp(bool increment, long timeout)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Stops any current ramp up/down in progress.
		/// </summary>
		public override void VolumeRampStop()
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Audio Component Callbacks

		private void Subscribe(VaddioAvBridgeAudioComponent audioComponent)
		{
			audioComponent.OnAudioMuteChanged += AudioComponentOnAudioMuteChanged;
			audioComponent.OnVolumeChanged += AudioComponentOnVolumeChanged;
		}

		private void Unsubscribe(VaddioAvBridgeAudioComponent audioComponent)
		{
			audioComponent.OnAudioMuteChanged -= AudioComponentOnAudioMuteChanged;
			audioComponent.OnVolumeChanged -= AudioComponentOnVolumeChanged;
		}

		private void AudioComponentOnAudioMuteChanged(object sender, BoolEventArgs boolEventArgs)
		{
			IsMuted = boolEventArgs.Data;
		}

		private void AudioComponentOnVolumeChanged(object sender, IntEventArgs intEventArgs)
		{
			VolumeLevel = intEventArgs.Data;
		}

		#endregion
	}
}
