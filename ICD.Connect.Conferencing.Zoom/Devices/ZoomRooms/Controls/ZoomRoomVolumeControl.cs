using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Audio;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Volume;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Controls
{
	public sealed class ZoomRoomVolumeControl : AbstractVolumeDeviceControl<ZoomRoom>
	{
		private const int INCREMENT_VALUE = 1;

		private readonly AudioComponent m_AudioComponent;
		private readonly VolumeComponent m_VolumeComponent;

		#region Properties

		/// <summary>
		/// Gets the minimum supported volume level.
		/// </summary>
		public override float VolumeLevelMin { get { return 0; } }

		/// <summary>
		/// Gets the maximum supported volume level.
		/// </summary>
		public override float VolumeLevelMax { get { return 100; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ZoomRoomVolumeControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			m_AudioComponent = parent.Components.GetComponent<AudioComponent>();
			m_VolumeComponent = parent.Components.GetComponent<VolumeComponent>();

			SupportedVolumeFeatures = eVolumeFeatures.Volume |
			                          eVolumeFeatures.VolumeAssignment |
			                          eVolumeFeatures.VolumeFeedback;

			Subscribe(m_AudioComponent);
			Subscribe(m_VolumeComponent);

			UpdateInputOutput();
			UpdateVolume();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_AudioComponent);
			Unsubscribe(m_VolumeComponent);
		}

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public override void ToggleIsMuted()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the raw volume level in the device volume representation.
		/// </summary>
		/// <param name="level"></param>
		public override void SetVolumeLevel(float level)
		{
			m_VolumeComponent.SetAudioOutputVolume((int)level);
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			m_VolumeComponent.SetAudioOutputVolume((int)VolumeLevel + INCREMENT_VALUE);
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			m_VolumeComponent.SetAudioOutputVolume((int)VolumeLevel - INCREMENT_VALUE);
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

		#region Private Methods

		/// <summary>
		/// When the audio input/output devices change force them back to our defaults.
		/// </summary>
		private void UpdateInputOutput()
		{
			AudioInputLine microphone = Parent.DefaultMicrophoneName == null ? null : m_AudioComponent.GetMicrophone(Parent.DefaultMicrophoneName);
			if (microphone != null)
				m_AudioComponent.SetAudioInputDeviceById(microphone.Id);

			AudioOutputLine speaker = Parent.DefaultSpeakerName == null ? null : m_AudioComponent.GetSpeaker(Parent.DefaultSpeakerName);
			if (speaker != null)
				m_AudioComponent.SetAudioOutputDeviceById(speaker.Id);
		}

		/// <summary>
		/// Gets the current volume level from the component.
		/// </summary>
		private void UpdateVolume()
		{
			VolumeLevel = m_VolumeComponent.AudioOutputVolume;
		}

		#endregion

		#region Audio Component Callbacks

		/// <summary>
		/// Subscribe to the audio component events.
		/// </summary>
		/// <param name="audioComponent"></param>
		private void Subscribe(AudioComponent audioComponent)
		{
			audioComponent.OnAudioInputDeviceChanged += VolumeComponentOnAudioInputDeviceChanged;
			audioComponent.OnAudioOutputDeviceChanged += VolumeComponentOnAudioOutputDeviceChanged;
		}

		/// <summary>
		/// Unsubscribe from the audio component events.
		/// </summary>
		/// <param name="audioComponent"></param>
		private void Unsubscribe(AudioComponent audioComponent)
		{
			audioComponent.OnAudioInputDeviceChanged -= VolumeComponentOnAudioInputDeviceChanged;
			audioComponent.OnAudioOutputDeviceChanged -= VolumeComponentOnAudioOutputDeviceChanged;
		}

		/// <summary>
		/// Called when the audio output device changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="stringEventArgs"></param>
		private void VolumeComponentOnAudioOutputDeviceChanged(object sender, StringEventArgs stringEventArgs)
		{
			UpdateInputOutput();
		}

		/// <summary>
		/// Called when the audio input device changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="stringEventArgs"></param>
		private void VolumeComponentOnAudioInputDeviceChanged(object sender, StringEventArgs stringEventArgs)
		{
			UpdateInputOutput();
		}

		#endregion

		#region Volume Component Callbacks

		/// <summary>
		/// Subscribe to the volume component events.
		/// </summary>
		/// <param name="volumeComponent"></param>
		private void Subscribe(VolumeComponent volumeComponent)
		{
			volumeComponent.OnOutputVolumeChanged += VolumeComponentOnOutputVolumeChanged;
		}

		/// <summary>
		/// Unsubscribe from the volume component events.
		/// </summary>
		/// <param name="volumeComponent"></param>
		private void Unsubscribe(VolumeComponent volumeComponent)
		{
			volumeComponent.OnOutputVolumeChanged -= VolumeComponentOnOutputVolumeChanged;
		}

		/// <summary>
		/// Called when the output volume changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void VolumeComponentOnOutputVolumeChanged(object sender, IntEventArgs e)
		{
			UpdateVolume();
		}

		#endregion
	}
}
