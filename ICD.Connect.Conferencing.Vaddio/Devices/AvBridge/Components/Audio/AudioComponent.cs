using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Audio
{
	public sealed class AudioComponent : AbstractAvBridgeComponent
	{
		#region Events

		public event EventHandler<IntEventArgs> OnVolumeChanged;

		public event EventHandler<BoolEventArgs> OnMuteChanged;

		#endregion

		private int m_Volume;

		private bool m_Mute;

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

		public bool Mute
		{
			get { return m_Mute; }
			private set
			{
				if (value == m_Mute)
					return;

				m_Mute = value;

				OnMuteChanged.Raise(this, new BoolEventArgs(value));
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
	}
}