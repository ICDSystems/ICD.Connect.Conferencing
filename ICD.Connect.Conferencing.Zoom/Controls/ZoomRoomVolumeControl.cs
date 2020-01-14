using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Conferencing.Zoom.Components.Volume;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomVolumeControl : AbstractVolumeDeviceControl<ZoomRoom>

	{
		private const int INCREMENT_VALUE = 5;

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

		public ZoomRoomVolumeControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			m_VolumeComponent = parent.Components.GetComponent<VolumeComponent>();

			SupportedVolumeFeatures = eVolumeFeatures.Volume |
			                          eVolumeFeatures.VolumeAssignment |
			                          eVolumeFeatures.VolumeFeedback;

			Subscribe(m_VolumeComponent);

			UpdateVolume();
		}

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_VolumeComponent);
		}

		#region Methods

		public override void SetIsMuted(bool mute)
		{
			throw new NotSupportedException();
		}

		public override void ToggleIsMuted()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
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

		public override void VolumeRamp(bool increment, long timeout)
		{
			throw new NotSupportedException();
		}

		public override void VolumeRampStop()
		{
			throw new NotSupportedException();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the current volume level from the component.
		/// </summary>
		private void UpdateVolume()
		{
			VolumeLevel = m_VolumeComponent.AudioOutputVolume;
		}

		#endregion

		#region Volume Component Callbacks

		private void Subscribe(VolumeComponent volumeComponent)
		{
			volumeComponent.OnOutputVolumeChanged += VolumeComponentOnOutputVolumeChanged;
		}

		private void Unsubscribe(VolumeComponent volumeComponent)
		{
			volumeComponent.OnOutputVolumeChanged -= VolumeComponentOnOutputVolumeChanged;
		}

		private void VolumeComponentOnOutputVolumeChanged(object sender, IntEventArgs e)
		{
			UpdateVolume();
		}

		#endregion
	}
}
