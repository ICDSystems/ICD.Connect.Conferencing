using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Audio
{
	public sealed class AudioComponent : AbstractCiscoComponent
	{

		private const string AUDIO_PATH = "Audio";

		public event EventHandler<IntEventArgs> OnVolumeChanged;

		public event EventHandler<BoolEventArgs> OnMuteChanged;

		private int m_Volume;

		private bool m_Mute;

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

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public AudioComponent(CiscoCodecDevice codec) : base(codec)
		{
			Subscribe(codec);
		}

		public void SetVolume(int volume)
		{
			if (volume < 0 || volume > 100)
			{
				Codec.Log(eSeverity.Warning, "Volume must be between 0 and 100, level: {0}", volume);
				return;
			}

			Codec.SendCommand("xCommand Audio Volume Set Level:{0}", volume);
		}

		public void SetMute(bool mute)
		{
			string muteString = mute ? "Mute" : "Unmute";
			Codec.SendCommand("xCommand Audio Volume {0}", muteString);
		}

		public void MuteToggle()
		{
			Codec.SendCommand("xCommand Audio Volume ToggleMute");
		}

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseVolume, CiscoCodecDevice.XSTATUS_ELEMENT, AUDIO_PATH, "Volume");
			codec.RegisterParserCallback(ParseMute, CiscoCodecDevice.XSTATUS_ELEMENT, AUDIO_PATH, "VolumeMute");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseVolume, CiscoCodecDevice.XSTATUS_ELEMENT, AUDIO_PATH, "Volume");
			codec.UnregisterParserCallback(ParseMute, CiscoCodecDevice.XSTATUS_ELEMENT, AUDIO_PATH, "VolumeMute");
		}


		private void ParseVolume(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			Volume = int.Parse(content);
		}

		private void ParseMute(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			Mute = content.Equals("On", StringComparison.InvariantCultureIgnoreCase);
		}
	}
}