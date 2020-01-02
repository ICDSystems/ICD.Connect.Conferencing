using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Audio;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecVolumeControl : AbstractVolumeDeviceControl<CiscoCodecDevice>
	{
		private const int INCREMENT_VALUE = 5;

		private readonly AudioComponent m_Component;

		#region Properties

		/// <summary>
		/// Returns the features that are supported by this volume control.
		/// </summary>
		public override eVolumeFeatures SupportedVolumeFeatures
		{
			get
			{
				return eVolumeFeatures.Mute |
					   eVolumeFeatures.MuteAssignment |
					   eVolumeFeatures.MuteFeedback |
					   eVolumeFeatures.Volume |
					   eVolumeFeatures.VolumeAssignment |
					   eVolumeFeatures.VolumeFeedback;
			}
		}

		/// <summary>
		/// Absolute Minimum the raw volume can be
		/// Used as a last resort for position calculation
		/// </summary>
		public override float VolumeLevelMin { get { return 0; } }

		/// <summary>
		/// Absolute Maximum the raw volume can be
		/// Used as a last resort for position calculation
		/// </summary>
		public override float VolumeLevelMax { get { return 100; } }

		#endregion

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Device this control belongs to</param>
		/// <param name="id">Id of this control in the device</param>
		public CiscoCodecVolumeControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_Component = parent.Components.GetComponent<AudioComponent>();

			Subscribe(m_Component);

			UpdateVolume();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_Component);
		}

		#region Methods

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetIsMuted(bool mute)
		{
			m_Component.SetMute(mute);
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public override void ToggleIsMuted()
		{
			m_Component.MuteToggle();
		}

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="level"></param>
		public override void SetVolumeLevel(float level)
		{
			m_Component.SetVolume((int)level);
		}

		/// <summary>
		/// Raises the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeIncrement()
		{
			m_Component.SetVolume(m_Component.Volume + INCREMENT_VALUE);
		}

		/// <summary>
		/// Lowers the volume one time
		/// Amount of the change varies between implementations - typically "1" raw unit
		/// </summary>
		public override void VolumeDecrement()
		{
			m_Component.SetVolume(m_Component.Volume - INCREMENT_VALUE);
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

		private void UpdateVolume()
		{
			VolumeLevel = m_Component.Volume;
			IsMuted = m_Component.Mute;
		}

		#endregion

		#region Audio Component Callbacks

		private void Subscribe(AudioComponent component)
		{
			if (component == null)
				return;

			component.OnVolumeChanged += ComponentOnVolumeChanged;
			component.OnMuteChanged += ComponentOnMuteChanged;
		}

		private void Unsubscribe(AudioComponent component)
		{
			if (component == null)
				return;

			component.OnVolumeChanged -= ComponentOnVolumeChanged;
			component.OnMuteChanged -= ComponentOnMuteChanged;
		}

		private void ComponentOnVolumeChanged(object sender, IntEventArgs args)
		{
			UpdateVolume();
		}

		private void ComponentOnMuteChanged(object sender, BoolEventArgs args)
		{
			UpdateVolume();
		}

		#endregion
	}
}
