using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Audio
{
	public sealed class AudioComponent : AbstractZoomRoomComponent
	{
		#region Events

		/// <summary>
		/// Raised when Software Audio Processing is enabled or disabled.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnSoftwareAudioProcessingChanged;

		/// <summary>
		/// Raised when the Reduce Reverb option is toggled.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnReduceReverbChanged;

		/// <summary>
		/// Raised when then selected audio input device is changed.
		/// </summary>
		public event EventHandler<StringEventArgs> OnAudioInputDeviceChanged;

		/// <summary>
		/// Raised when the selected audio output device is changed.
		/// </summary>
		public event EventHandler<StringEventArgs> OnAudioOutputDeviceChanged;

		/// <summary>
		/// Raised when the collection of microphones is changed.
		/// </summary>
		public event EventHandler OnMicrophonesChanged;

		/// <summary>
		/// Raised when the collection of speakers is changed.
		/// </summary>
		public event EventHandler OnSpeakersChanged;

		#endregion

		private readonly IcdOrderedDictionary<string, AudioInputLine> m_Microphones;
		private readonly IcdOrderedDictionary<string, AudioOutputLine> m_Speakers;

		private bool m_IsSapDisabled;
		private bool m_ReduceReverb;
		private string m_SelectedAudioInputDeviceId;
		private string m_SelectedAudioOutputDeviceId;

		#region Properties

		/// <summary>
		/// True for when Software Audio Processing is disabled, otherwise false.
		/// </summary>
		public bool IsSapDisabled
		{
			get { return m_IsSapDisabled; }
			private set
			{
				if (m_IsSapDisabled == value)
					return;

				m_IsSapDisabled = value;
				Parent.Logger.Set("SAP Disabled", eSeverity.Informational, m_IsSapDisabled);
				OnSoftwareAudioProcessingChanged.Raise(this, new BoolEventArgs(m_IsSapDisabled));
			}
		}

		/// <summary>
		/// True for when the Reduce Reverb option is enabled, otherwise false.
		/// </summary>
		public bool ReduceReverb
		{
			get { return m_ReduceReverb; }
			private set
			{
				if (m_ReduceReverb == value)
					return;

				m_ReduceReverb = value;
				Parent.Logger.Set("Reduce Reverb", eSeverity.Informational, m_ReduceReverb);
				OnReduceReverbChanged.Raise(this, new BoolEventArgs(m_ReduceReverb));
			}
		}

		/// <summary>
		/// The currently selected Audio Input Device Id for the Zoom Room.
		/// </summary>
		public string SelectedAudioInputDeviceId
		{
			get { return m_SelectedAudioInputDeviceId; }
			private set
			{
				if (m_SelectedAudioInputDeviceId == value)
					return;

				m_SelectedAudioInputDeviceId = value;
				Parent.Logger.Set("Selected Audio Input DeviceId", eSeverity.Informational, m_SelectedAudioInputDeviceId);
				OnAudioInputDeviceChanged.Raise(this, new StringEventArgs(m_SelectedAudioInputDeviceId));
			}
		}

		/// <summary>
		/// The currently selected Audio Output Device Id for the Zoom Room.
		/// </summary>
		public string SelectedAudioOutputDeviceId
		{
			get { return m_SelectedAudioOutputDeviceId; }
			private set
			{
				if (m_SelectedAudioOutputDeviceId == value)
					return;

				m_SelectedAudioOutputDeviceId = value;
				Parent.Logger.Set("Selected Audio Output DeviceId", eSeverity.Informational, m_SelectedAudioOutputDeviceId);
				OnAudioOutputDeviceChanged.Raise(this, new StringEventArgs(m_SelectedAudioOutputDeviceId));
			}
		}

		#endregion

		#region Constructor

		public AudioComponent(ZoomRoom parent)
			: base(parent)
		{
			m_Microphones = new IcdOrderedDictionary<string, AudioInputLine>();
			m_Speakers = new IcdOrderedDictionary<string, AudioOutputLine>();

			Subscribe(Parent);
		}

		protected override void DisposeFinal()
		{
			base.DisposeFinal();

			OnSoftwareAudioProcessingChanged = null;
			OnReduceReverbChanged = null;
			OnMicrophonesChanged = null;
			OnSpeakersChanged = null;

			Unsubscribe(Parent);
		}

		#endregion

		#region Methods

		protected override void Initialize()
		{
			base.Initialize();

			Parent.SendCommand("zConfiguration Audio Input is_sap_disabled");
			Parent.SendCommand("zConfiguration Audio Input reduce_reverb");
			Parent.SendCommand("zStatus Audio Input Line");
			Parent.SendCommand("zStatus Audio Output Line");
			Parent.SendCommand("zConfiguration Audio Input selectedId");
			Parent.SendCommand("zConfiguration Audio Output selectedId");
		}

		public IEnumerable<AudioInputLine> GetMicrophones()
		{
			return m_Microphones.Values.ToArray(m_Microphones.Count);
		}

		public IEnumerable<AudioOutputLine> GetSpeakers()
		{
			return m_Speakers.Values.ToArray(m_Speakers.Count);
		}

		public void SetSapDisabled(bool disabled)
		{
			Parent.Logger.Log(eSeverity.Informational, "Setting SAP disabled to: {0}", disabled);
			Parent.SendCommand("zConfiguration Audio Input is_sap_disabled: {0}", disabled ? "on" : "off");
		}

		public void SetReduceReverb(bool enabled)
		{
			Parent.Logger.Log(eSeverity.Informational, "Setting Reduce Reverb to: {0}", enabled);
			Parent.SendCommand("zConfiguration Audio Input reduce_reverb: {0}", enabled ? "on" : "off");
		}

		public void SetAudioInputDeviceById(string id)
		{
			Parent.Logger.Log(eSeverity.Informational, "Setting Audio Input Device Id to: {0}", id);
			Parent.SendCommand("zConfiguration Audio Input selectedId: {0}", id);
		}

		public void SetAudioOutputDeviceById(string id)
		{
			Parent.Logger.Log(eSeverity.Informational, "Setting Audio Output Device Id to: {0}", id);
			Parent.SendCommand("zConfiguration Audio Output selectedId: {0}", id);
		}

		#endregion

		#region Parent Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<AudioConfigurationResponse>(AudioConfigurationResponseCallback);
			parent.RegisterResponseCallback<AudioInputLineResponse>(AudioInputLineResponseCallback);
			parent.RegisterResponseCallback<AudioOutputLineResponse>(AudioOutputLineResponseCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<AudioConfigurationResponse>(AudioConfigurationResponseCallback);
			parent.UnregisterResponseCallback<AudioInputLineResponse>(AudioInputLineResponseCallback);
			parent.UnregisterResponseCallback<AudioOutputLineResponse>(AudioOutputLineResponseCallback);
		}

		private void AudioConfigurationResponseCallback(ZoomRoom zoomroom, AudioConfigurationResponse response)
		{
			AudioConfiguration topData = response.AudioConfiguration;
			if (topData == null)
				return;

			InputConfiguration inputData = topData.InputConfiguration;
			if (inputData != null)
			{
				if (inputData.IsSapDisabled != null)
					IsSapDisabled = (bool)inputData.IsSapDisabled;

				if (inputData.ReduceReverb != null)
					ReduceReverb = (bool)inputData.ReduceReverb;

				if (inputData.SelectedId != null)
					SelectedAudioInputDeviceId = inputData.SelectedId;
			}

			OutputConfiguration outputData = topData.OutputConfiguration;
			if (outputData != null)
			{
				if (outputData.SelectedId != null)
					SelectedAudioOutputDeviceId = outputData.SelectedId;
			}
		}

		private void AudioInputLineResponseCallback(ZoomRoom zoomroom, AudioInputLineResponse response)
		{
			var data = response.AudioInputLines;
			if (data == null)
				return;

			m_Microphones.Clear();
			m_Microphones.AddRange(data.Select(m => new KeyValuePair<string, AudioInputLine>(m.Id, m)));

			OnMicrophonesChanged.Raise(this);
		}

		private void AudioOutputLineResponseCallback(ZoomRoom zoomroom, AudioOutputLineResponse response)
		{
			var data = response.AudioOutputLines;
			if (data == null)
				return;

			m_Speakers.Clear();
			m_Speakers.AddRange(data.Select(s => new KeyValuePair<string, AudioOutputLine>(s.Id, s)));

			OnSpeakersChanged.Raise(this);
		}

		#endregion

		#region Console

		public override string ConsoleHelp { get { return "Zoom Room Audio"; } }

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("IsSapDisabled", IsSapDisabled);
			addRow("ReduceReverb", ReduceReverb);
			addRow("SelectedAudioInputDeviceId", SelectedAudioOutputDeviceId);
			addRow("SelectedAudioOutputDeviceId", SelectedAudioOutputDeviceId);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetSapDisabled", "SetSapDisabled <true/false>",
			                                             b => SetSapDisabled(b));
			yield return new GenericConsoleCommand<bool>("SetReduceReverb", "SetReduceReverb <true/false>",
			                                             b => SetReduceReverb(b));
			yield return new GenericConsoleCommand<string>("SetAudioInputById",
			                                               "SetAudioInputById <Audio Input Zoom Device Id>",
			                                               s => SetAudioInputDeviceById(s));
			yield return new GenericConsoleCommand<string>("SetAudioOutputById",
			                                               "SetAudioOutputById <Audio Output Zoom Device Id>",
			                                               s => SetAudioOutputDeviceById(s));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
