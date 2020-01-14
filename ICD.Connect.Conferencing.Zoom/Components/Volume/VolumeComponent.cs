using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Volume
{
	public sealed class VolumeComponent : AbstractZoomRoomComponent
	{
		#region Events

		/// <summary>
		/// Raised when the audio input (ex: a microphone) is changed.
		/// </summary>
		public event EventHandler<IntEventArgs> OnInputVolumeChanged;

		/// <summary>
		/// Raised when the audio output (ex: speakers) is changed.
		/// </summary>
		public event EventHandler<IntEventArgs> OnOutputVolumeChanged;

		#endregion

		#region Fields

		private int m_AudioInputVolume;
		private int m_AudioOutputVolume;

		#endregion

		#region Properties

		/// <summary>
		/// The volume for the currently selected audio input device.
		/// </summary>
		public int AudioInputVolume
		{
			get { return m_AudioInputVolume; }
			private set
			{
				if (m_AudioInputVolume == value)
					return;

				m_AudioInputVolume = value;
				Parent.Log(eSeverity.Informational, "Audio Input Volume changed to: {0}", m_AudioInputVolume);
				OnInputVolumeChanged.Raise(this, new IntEventArgs(m_AudioInputVolume));
			}
		}

		/// <summary>
		/// The volume for the currently selected audio output device.
		/// </summary>
		public int AudioOutputVolume
		{
			get { return m_AudioOutputVolume; }
			private set
			{
				if (m_AudioOutputVolume == value)
					return;

				m_AudioOutputVolume = value;
				Parent.Log(eSeverity.Informational, "Audio Output Volume changed to: {0}", m_AudioOutputVolume);
				OnOutputVolumeChanged.Raise(this, new IntEventArgs(m_AudioOutputVolume));
			}
		}

		#endregion

		#region Constructor

		public VolumeComponent(ZoomRoom parent)
			: base(parent)
		{
			Subscribe(Parent);
		}

		protected override void DisposeFinal()
		{
			base.DisposeFinal();

			OnInputVolumeChanged = null;
			OnOutputVolumeChanged = null;

			Unsubscribe(Parent);
		}

		#endregion

		#region Methods

		protected override void Initialize()
		{
			base.Initialize();
			UpdateVolume();
		}

		/// <summary>
		/// Sends a command to the ZoomRoom to change the audio input volume.
		/// </summary>
		/// <param name="volume"></param>
		public void SetAudioInputVolume(int volume)
		{
			Parent.Log(eSeverity.Informational, "Setting Audio Input Volume to: {0}", volume);
			Parent.SendCommand("zConfiguration Audio Input volume: {0}", volume);
		}

		/// <summary>
		/// Sends a command to the ZoomRoom to change the audio output volume.
		/// </summary>
		/// <param name="volume"></param>
		public void SetAudioOutputVolume(int volume)
		{
			Parent.Log(eSeverity.Informational, "Setting Audio Output Volume to: {0}", volume);
			Parent.SendCommand("zConfiguration Audio Output volume: {0}", volume);
		}

		/// <summary>
		/// Polls the current input & output volume levels.
		/// </summary>
		private void UpdateVolume()
		{
			Parent.SendCommand("zConfiguration Audio Input volume");
			Parent.SendCommand("zConfiguration Audio Output volume");
		}

		#endregion

		#region Parent Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<AudioConfigurationResponse>(AudioConfigurationResponseCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<AudioConfigurationResponse>(AudioConfigurationResponseCallback);
		}

		private void AudioConfigurationResponseCallback(ZoomRoom zoomroom, AudioConfigurationResponse response)
		{
			var audioData = response.AudioConfiguration;
			if (audioData == null)
				return;

			var inputData = audioData.InputConfiguration;
			if (inputData != null)
				AudioInputVolume = inputData.Volume != null ? (int)inputData.Volume : AudioInputVolume;

			var outputData = audioData.OutputConfiguration;
			if (outputData != null)
				AudioOutputVolume = outputData.Volume != null ? (int)outputData.Volume : AudioOutputVolume;
		}

		#endregion

		#region Console

		public override string ConsoleHelp { get { return "Zoom Room Volume"; } }

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("AudioInputVolume", AudioInputVolume);
			addRow("AudioOutputVolume", AudioOutputVolume);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
			{
				yield return command;
			}

			yield return new GenericConsoleCommand<int>("SetAudioInput", "SetAudioInputVolume <int [0-100]>",
			                                            i => SetAudioInputVolume(i));
			yield return new GenericConsoleCommand<int>("SetAudioOutput", "SetAudioOutputVolume <int [0-100]>",
			                                            i => SetAudioOutputVolume(i));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
