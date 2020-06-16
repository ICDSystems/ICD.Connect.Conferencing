using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Audio;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Components.Video;

namespace ICD.Connect.Conferencing.Vaddio.Devices.AvBridge.Controls.Conferencing
{
	public sealed class VaddioAvBridgeConferenceControl : AbstractConferenceDeviceControl<VaddioAvBridgeDevice, TraditionalConference>
	{
		#region Events

		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;
		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		#endregion

		private readonly VaddioAvBridgeAudioComponent m_AudioComponent;
		private readonly VaddioAvBridgeVideoComponent m_VideoComponent;

		[CanBeNull]
		private TraditionalConference m_ActiveConference;

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Audio | eCallType.Video; } }

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public VaddioAvBridgeConferenceControl(VaddioAvBridgeDevice parent, int id) 
			: base(parent, id)
		{
			SupportedConferenceFeatures = eConferenceFeatures.PrivacyMute;

			m_AudioComponent = parent.Components.GetComponent<VaddioAvBridgeAudioComponent>();
			m_VideoComponent = parent.Components.GetComponent<VaddioAvBridgeVideoComponent>();

			Subscribe(m_AudioComponent);
			Subscribe(m_VideoComponent);
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(m_AudioComponent);
			Unsubscribe(m_VideoComponent);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Starts a new conference and adds a participant with the given name.
		/// </summary>
		/// <param name="name"></param>
		public void StartConference(string name)
		{
			EndConference();

			DateTime now = IcdEnvironment.GetUtcTime();

			ThinTraditionalParticipant participant = new ThinTraditionalParticipant
			{
				HangupCallback = HangupParticipant
			};
			participant.SetName(name);
			participant.SetAnswerState(eCallAnswerState.Answered);
			participant.SetCallType(Supports);
			participant.SetDialTime(now);
			participant.SetStart(now);
			participant.SetStatus(eParticipantStatus.Connected);

			m_ActiveConference = new TraditionalConference();
			m_ActiveConference.AddParticipant(participant);

			OnConferenceAdded.Raise(this, new ConferenceEventArgs(m_ActiveConference));
		}

		/// <summary>
		/// Stops the current conference.
		/// </summary>
		public void EndConference()
		{
			if (m_ActiveConference == null)
				return;

			m_ActiveConference.Hangup();

			var endedConference = m_ActiveConference;
			m_ActiveConference = null;

			OnConferenceRemoved.Raise(this, new ConferenceEventArgs(endedConference));
		}

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<TraditionalConference> GetConferences()
		{
			if (m_ActiveConference != null)
				yield return m_ActiveConference;
		}

		/// <summary>
		/// Returns the level of support the device has for the given context.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			return eDialContextSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given context.
		/// </summary>
		/// <param name="dialContext"></param>
		public override void Dial(IDialContext dialContext)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetPrivacyMute(bool enabled)
		{
			m_AudioComponent.SetAudioMute(enabled);
		}

		/// <summary>
		/// Sets whether the camera should transmit video or not.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetCameraEnabled(bool enabled)
		{
			m_VideoComponent.SetVideoMute(enabled);
		}

		#endregion

		#region Private Methods

		private void HangupParticipant(ThinTraditionalParticipant participant)
		{
			participant.SetStatus(eParticipantStatus.Disconnected);
			participant.SetEnd(IcdEnvironment.GetUtcTime());

			if (m_ActiveConference != null)
				m_ActiveConference.RemoveParticipant(participant);
		}

		#endregion

		#region Audio Component Callbacks

		private void Subscribe(VaddioAvBridgeAudioComponent audioComponent)
		{
			audioComponent.OnAudioMuteChanged += AudioComponentOnAudioMuteChanged;
		}

		private void Unsubscribe(VaddioAvBridgeAudioComponent audioComponent)
		{
			audioComponent.OnAudioMuteChanged -= AudioComponentOnAudioMuteChanged;
		}

		private void AudioComponentOnAudioMuteChanged(object sender, BoolEventArgs e)
		{
			PrivacyMuted = e.Data;
		}

		#endregion

		#region Video Component Callbacks

		private void Subscribe(VaddioAvBridgeVideoComponent videoComponent)
		{
			videoComponent.OnVideoMuteChanged += VideoComponentOnVideoMuteChanged;
		}

		private void Unsubscribe(VaddioAvBridgeVideoComponent videoComponent)
		{
			videoComponent.OnVideoMuteChanged -= VideoComponentOnVideoMuteChanged;
		}

		private void VideoComponentOnVideoMuteChanged(object sender, BoolEventArgs e)
		{
			CameraEnabled = e.Data;
		}

		#endregion

		#region Console

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<string>("StartConference", "StartConference <Name>", n => StartConference(n));
			yield return new ConsoleCommand("EndConference", "Ends the active conference", () => EndConference());
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}