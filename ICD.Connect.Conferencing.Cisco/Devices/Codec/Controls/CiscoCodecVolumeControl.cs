using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Audio.Console.Mute;
using ICD.Connect.Audio.Controls.Mute;
using ICD.Connect.Audio.Controls.Volume;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Audio;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	public sealed class CiscoCodecVolumeControl : AbstractVolumeLevelDeviceControl<CiscoCodecDevice>, IVolumeMuteFeedbackDeviceControl
	{
		private const float INCREMENT_VALUE = 5;

		private readonly AudioComponent m_Component;

		public event EventHandler<BoolEventArgs> OnMuteStateChanged;

		/// <summary>
		/// Gets the current volume, in the parent device's format
		/// </summary>
		public override float VolumeLevel { get { return m_Component.Volume; } }

		/// <summary>
		/// Absolute Minimum the raw volume can be
		/// Used as a last resort for position caculation
		/// </summary>
		protected override float VolumeRawMinAbsolute { get { return 0; } }

		/// <summary>
		/// Absolute Maximum the raw volume can be
		/// Used as a last resport for position caculation
		/// </summary>
		protected override float VolumeRawMaxAbsolute { get { return 100; } }

		/// <summary>
		/// Sets the raw volume. This will be clamped to the min/max and safety min/max.
		/// </summary>
		/// <param name="volume"></param>
		public override void SetVolumeLevel(float volume)
		{
			m_Component.SetVolume((int)volume);
		}

		/// <summary>
		/// Gets the muted state.
		/// </summary>
		public bool VolumeIsMuted
		{
			get { return m_Component.Mute; }
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="parent">Device this control belongs to</param>
		/// <param name="id">Id of this control in the device</param>
		public CiscoCodecVolumeControl(CiscoCodecDevice parent, int id) : base(parent, id)
		{
			IncrementValue = INCREMENT_VALUE;
			
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
			VolumeFeedback(args.Data);
		}

		private void ComponentOnMuteChanged(object sender, BoolEventArgs args)
		{
			OnMuteStateChanged.Raise(this, args);
		}

		private void UpdateVolume()
		{
			VolumeFeedback(m_Component.Volume);

			if (m_Component.Mute)
				OnMuteStateChanged.Raise(this, new BoolEventArgs(m_Component.Mute));
		}

		/// <summary>
		/// Toggles the current mute state.
		/// </summary>
		public void VolumeMuteToggle()
		{
			m_Component.MuteToggle();
		}

		/// <summary>
		/// Sets the mute state.
		/// </summary>
		/// <param name="mute"></param>
		public void SetVolumeMute(bool mute)
		{
			m_Component.SetMute(mute);
		}


		#region console

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			VolumeMuteFeedbackDeviceControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VolumeMuteBasicDeviceControlConsole.GetConsoleCommands(this))
				yield return command;
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}