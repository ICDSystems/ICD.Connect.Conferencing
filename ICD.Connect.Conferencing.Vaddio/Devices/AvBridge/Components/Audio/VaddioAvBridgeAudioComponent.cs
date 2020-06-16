using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Audio
{
	public sealed class VaddioAvBridgeAudioComponent : AbstractVaddioAvBridgeComponent
	{
		#region Events

		public event EventHandler<GenericEventArgs<eAudioInput>> OnAudioInputChanged; 

		public event EventHandler<IntEventArgs> OnVolumeChanged;

		public event EventHandler<BoolEventArgs> OnAudioMuteChanged;

		#endregion

		private eAudioInput m_AudioInput;

		private int m_Volume;

		private bool m_AudioMute;

		#region Properties

		public eAudioInput AudioInput
		{
			get { return m_AudioInput; }
			private set
			{
				if (value == m_AudioInput)
					return;

				m_AudioInput = value;

				AvBridge.Logger.LogSetTo(eSeverity.Informational, "AudioInput", m_AudioInput);
				OnAudioInputChanged.Raise(this, new GenericEventArgs<eAudioInput>(value));
			}
		}

		public int Volume
		{
			get { return m_Volume; }
			private set
			{
				if (value == m_Volume)
					return;

				m_Volume = value;

				AvBridge.Logger.LogSetTo(eSeverity.Informational, "Volume", m_Volume);
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

				AvBridge.Logger.LogSetTo(eSeverity.Informational, "AudioMute", m_AudioMute);
				OnAudioMuteChanged.Raise(this, new BoolEventArgs(value));
			}
		}

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="avBridge"></param>
		public VaddioAvBridgeAudioComponent(VaddioAvBridgeDevice avBridge)
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
			GetAudioMute();
			GetAudioVolume();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the current audio input source.
		/// </summary>
		public void GetAudioInput()
		{
			AvBridge.Logger.Log(eSeverity.Informational, "Querying Audio Input State");
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

			AvBridge.Logger.Log(eSeverity.Informational, "Setting Audio Input to {0}", source);
			AvBridge.SendCommand("audio input " + source);
			GetAudioInput();
		}

		/// <summary>
		/// Gets the current audio mute state.
		/// </summary>
		public void GetAudioMute()
		{
			AvBridge.Logger.Log(eSeverity.Informational, "Querying Audio Mute State");
			AvBridge.SendCommand("audio mute get");
		}

		/// <summary>
		/// Sets the current audio mute state.
		/// </summary>
		public void SetAudioMute(bool mute)
		{
			string muteString = mute ? "on" : "off";

			AvBridge.Logger.Log(eSeverity.Informational, "Setting Audio Mute State to {0}", muteString);
			AvBridge.SendCommand("audio mute " + muteString);
			GetAudioMute();
		}

		/// <summary>
		/// Toggle the audio mute state.
		/// </summary>
		public void ToggleAudioMute()
		{
			AvBridge.Logger.Log(eSeverity.Informational, "Toggling Audio Mute State");
			AvBridge.SendCommand("audio mute toggle");
			GetAudioMute();
		}

		/// <summary>
		/// Gets the current audio volume level.
		/// </summary>
		public void GetAudioVolume()
		{
			AvBridge.Logger.Log(eSeverity.Informational, "Querying Audio Volume State");
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

			AvBridge.Logger.Log(eSeverity.Informational, "Setting Audio Volume to {0}", volume);
			AvBridge.SendCommand("audio volume set {0}", volume);
			GetAudioVolume();
		}

		/// <summary>
		/// Increments the audio volume one step.
		/// </summary>
		public void IncrementAudioVolume()
		{
			AvBridge.Logger.Log(eSeverity.Informational, "Incrementing Audio Volume One Step");
			AvBridge.SendCommand("audio volume up");
			GetAudioVolume();
		}

		/// <summary>
		/// Decrements the audio volume one step.
		/// </summary>
		public void DecrementAudioVolume()
		{
			AvBridge.Logger.Log(eSeverity.Informational, "Decrementing Audio Volume One Step");
			AvBridge.SendCommand("audio volume down");
			GetAudioVolume();
		}

		#endregion

		#region Feedback Handlers

		protected override void Subscribe(VaddioAvBridgeDevice avBridge)
		{
			base.Subscribe(avBridge);

			avBridge.RegisterFeedback("audio input", HandleAudioInputFeedback);
			avBridge.RegisterFeedback("audio mute", HandleAudioMuteFeedback);
			avBridge.RegisterFeedback("audio volume", HandleAudioVolumeFeedback);
		}

		private void HandleAudioInputFeedback(VaddioAvBridgeSerialResponse response)
		{
			if (response.CommandSetValue == "get")
				AudioInput = (eAudioInput)Enum.Parse(typeof(eAudioInput), response.OptionValue, true);
		}

		private void HandleAudioMuteFeedback(VaddioAvBridgeSerialResponse response)
		{
			if (response.CommandSetValue == "get")
				AudioMute = response.OptionValue == "on";
		}

		private void HandleAudioVolumeFeedback(VaddioAvBridgeSerialResponse response)
		{
			if (response.CommandSetValue == "get")
				Volume = int.Parse(response.OptionValue);
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

			yield return new GenericConsoleCommand<eAudioInput>("SetAudioInput", "<balanced|unbalanced>", i => SetAudioInput(i));
			yield return new GenericConsoleCommand<bool>("SetAudioMute", "<true|false>", m => SetAudioMute(m));
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

			addRow("Input", AudioInput);
			addRow("Volume", Volume);
			addRow("AudioMute", AudioMute);
		}

		#endregion
	}
}
