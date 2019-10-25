using System;
using System.Collections.Generic;
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

		#endregion

		private bool m_IsSapDisabled;
		private bool m_ReduceReverb;

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
				Parent.Log(eSeverity.Informational, "IsSapDisabled changed to: {0}", m_IsSapDisabled);
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
				Parent.Log(eSeverity.Informational, "ReduceReverb changed to: {0}", m_ReduceReverb);
				OnReduceReverbChanged.Raise(this, new BoolEventArgs(m_ReduceReverb));
			}
		}

		#endregion

		#region Constructor

		public AudioComponent(ZoomRoom parent) 
			: base(parent)
		{
			Subscribe(Parent);
		}

		protected override void DisposeFinal()
		{
			base.DisposeFinal();

			OnSoftwareAudioProcessingChanged = null;
			OnReduceReverbChanged = null;

			Unsubscribe(Parent);
		}

		#endregion

		#region Methods

		protected override void Initialize()
		{
			base.Initialize();

			UpdateAudio();
		}

		public void SetSapDisabled(bool disabled)
		{
			Parent.Log(eSeverity.Informational, "Setting SAP disabled to: {0}", disabled);
			Parent.SendCommand("zConfiguration Audio Input is_sap_disabled: {0}", disabled ? "on" : "off");
		}

		public void SetReduceReverb(bool enabled)
		{
			Parent.Log(eSeverity.Informational, "Setting Reduce Reverb to: {0}", enabled);
			Parent.SendCommand("zConfiguration Audio Input reduce_reverb: {0}", enabled ? "on" : "off");
		}

		public void UpdateAudio()
		{
			Parent.SendCommand("zConfiguration Audio Input is_sap_disabled");
			Parent.SendCommand("zConfiguration Audio Input reduce_reverb");
		}

		#endregion

		#region Parent Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			Parent.RegisterResponseCallback<AudioConfigurationResponse>(AudioConfigurationResponseCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			Parent.UnregisterResponseCallback<AudioConfigurationResponse>(AudioConfigurationResponseCallback);
		}

		private void AudioConfigurationResponseCallback(ZoomRoom zoomroom, AudioConfigurationResponse response)
		{
			var topData = response.AudioInputConfiguration;
			if (topData == null)
				return;

			var data = topData.InputConfiguration;
			if (data == null)
				return;

			IsSapDisabled = data.IsSapDisabled;
			ReduceReverb = data.ReduceReverb;
		}

		#endregion

		#region Console

		public override string ConsoleHelp { get { return "Zoom Room Audio"; } }

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("IsSapDisabled", IsSapDisabled);
			addRow("ReduceReverb", ReduceReverb);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<bool>("SetSapDisabled", "SetSapDisabled <true/false>",
			                                             b => SetSapDisabled(b));
			yield return new GenericConsoleCommand<bool>("SetReduceReverb", "SetReduceReverb <true/false>",
			                                             b => SetReduceReverb(b));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
