using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Audio
{
	public sealed class AudioComponent : AbstractAvBridgeComponent
	{
		#region Events

		public event EventHandler<IntEventArgs> OnVolumeChanged;

		public event EventHandler<BoolEventArgs> OnAudioMuteChanged;

		#endregion

		private int m_Volume;

		private bool m_AudioMute;

		#region Properties

		public int Volume
		{
			get { return m_Volume; }
			private set
			{
				if (value == m_Volume)
					return;

				m_Volume = value;

				OnVolumeChanged.Raise(this, new IntEventArgs(value));
			}
		}

		public bool AudioMute
		{
			get { return m_AudioMute; }
			private set
			{
				if (value == m_AudioMute)
					return;

				m_AudioMute = value;

				OnAudioMuteChanged.Raise(this, new BoolEventArgs(value));
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="avBridge"></param>
		public AudioComponent(VaddioAvBridgeDevice avBridge) 
			: base(avBridge)
		{
			Subscribe(avBridge);

			if(avBridge.Initialized)
				Initialize();
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			GetAudioInput();
			GetAudioVolume();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the current audio input source.
		/// </summary>
		public void GetAudioInput()
		{
			AvBridge.SendCommand("audio input get");
		}

		/// <summary>
		/// Sets the current audio input source.
		/// </summary>
		/// <param name="source"></param>
		public void SetAudioInput(eAudioInput source)
		{
			if (source < eAudioInput.balanced)
			{
				AvBridge.Logger.Log(eSeverity.Warning, "Audio input must either be set to balanced or unbalanced");
				return;
			}

			AvBridge.SendCommand("audio input " + source);
		}

		/// <summary>
		/// Gets the current audio mute state.
		/// </summary>
		public void GetAudioMute()
		{
			AvBridge.SendCommand("audio mute get");
		}

		/// <summary>
		/// Sets the current audio mute state.
		/// </summary>
		public void SetAudioMute(bool mute)
		{
			string muteString = mute ? "on" : "off";
			AvBridge.SendCommand("audio mute " + muteString);
		}

		/// <summary>
		/// Toggle the audio mute state.
		/// </summary>
		public void ToggleAudioMute()
		{
			AvBridge.SendCommand("audio mute toggle");
		}

		/// <summary>
		/// Gets the current audio volume level.
		/// </summary>
		public void GetAudioVolume()
		{
			AvBridge.SendCommand("audio volume get");
		}

		/// <summary>
		/// Sets the current audio volume level
		/// </summary>
		/// <param name="volume">The volume level to be set, should be between 0-10 inclusive.</param>
		public void SetAudioVolume(int volume)
		{
			if (volume < 0 || volume > 10)
			{
				AvBridge.Logger.Log(eSeverity.Warning, "Volume must be between 0 and 10, level: {0}", volume);
				return;
			}

			AvBridge.SendCommand("audio volume set {0}");
		}

		/// <summary>
		/// Increments the audio volume one step.
		/// </summary>
		public void IncrementAudioVolume()
		{
			AvBridge.SendCommand("audio volume up");
		}

		/// <summary>
		/// Decrements the audio volume one step.
		/// </summary>
		public void DecrementAudioVolume()
		{
			AvBridge.SendCommand("audio volume down");
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetAudioMute", "<true | false>", m => SetAudioMute(m));
			yield return new ConsoleCommand("ToggleAudioMute", "Toggles the audio mute", () => ToggleAudioMute());
			yield return
				new GenericConsoleCommand<int>("SetAudioVolume", "Sets the volume [0-10] inclusive", v => SetAudioVolume(v));
			yield return new ConsoleCommand("IncrementVolume", "Increments the volume one step", () => IncrementAudioVolume());
			yield return new ConsoleCommand("DecrementVolume", "Decrements the volume one step", () => DecrementAudioVolume());
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Volume", Volume);
			addRow("AudioMute", AudioMute);
		}

		#endregion
	}
}