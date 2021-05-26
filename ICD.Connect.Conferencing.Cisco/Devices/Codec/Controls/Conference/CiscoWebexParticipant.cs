using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Conference;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference
{
	public sealed class CiscoWebexParticipant : AbstractParticipant
	{
		private WebexParticipantInfo m_Info;
		private eCallRecordingStatus m_RecordingStatus;

		private readonly int m_CallId;
		private readonly ConferenceComponent m_ConferenceComponent;

		public override IRemoteCamera Camera { get { return null; } }

		public string WebexParticipantId { get { return m_Info.ParticipantId; } }

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="info"></param>
		/// <param name="callId"></param>
		/// <param name="conferenceComponent"></param>
		public CiscoWebexParticipant(WebexParticipantInfo info, int callId, [NotNull] ConferenceComponent conferenceComponent)
		{
			if (conferenceComponent == null)
				throw new ArgumentNullException("conferenceComponent");

			UpdateInfo(info);
			m_CallId = callId;

			m_ConferenceComponent = conferenceComponent;
			Subscribe(m_ConferenceComponent);

			CallType = eCallType.Audio | eCallType.Video;
			StartTime = IcdEnvironment.GetUtcTime();
			DialTime = IcdEnvironment.GetUtcTime();
			AnswerState = eCallAnswerState.Answered;

			SupportedParticipantFeatures = eParticipantFeatures.GetName |
			                               eParticipantFeatures.GetCallType |
			                               eParticipantFeatures.GetStatus |
			                               eParticipantFeatures.GetIsMuted |
			                               eParticipantFeatures.GetIsSelf |
			                               eParticipantFeatures.GetIsHost |
			                               eParticipantFeatures.Kick |
			                               eParticipantFeatures.SetMute;
		}

		protected override void DisposeFinal()
		{
			Unsubscribe(m_ConferenceComponent);

			base.DisposeFinal();
		}

		#endregion

		#region Methods

		public override void Hold()
		{
			throw new NotSupportedException();
		}

		public override void Resume()
		{
			throw new NotSupportedException();
		}

		public override void Hangup()
		{
			m_ConferenceComponent.ParticipantDisconnect(m_CallId, m_Info.ParticipantId);
		}

		public override void SendDtmf(string data)
		{
			throw new NotSupportedException();
		}

		public override void Kick()
		{
			m_ConferenceComponent.ParticipantDisconnect(m_CallId, m_Info.ParticipantId);
		}

		public override void Mute(bool mute)
		{
			m_ConferenceComponent.ParticipantMute(mute, m_CallId, m_Info.ParticipantId);
		}

		public override void ToggleHandRaise()
		{
			if (!IsSelf)
				return;

			if (m_Info.HandRaised)
				m_ConferenceComponent.LowerHand(m_CallId);
			else
				m_ConferenceComponent.RaiseHand(m_CallId);
		}

		public override void RecordCallAction(bool stop)
		{
			if (!CanRecord)
				throw new InvalidOperationException("Participant currently cannot record.");

			switch (m_RecordingStatus)
			{
				case eCallRecordingStatus.None:
					m_ConferenceComponent.RecordingStart(m_CallId);
					break;
				case eCallRecordingStatus.Recording:
					if (stop)
						m_ConferenceComponent.RecordingStop(m_CallId);
					else
						m_ConferenceComponent.RecordingPause(m_CallId);
					break;
				case eCallRecordingStatus.Paused:
					if (stop)
						m_ConferenceComponent.RecordingStop(m_CallId);
					else
						m_ConferenceComponent.RecordingResume(m_CallId);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		#region Private Methods

		private void UpdateInfo(WebexParticipantInfo newInfo)
		{
			m_Info = newInfo;

			Name = m_Info.DisplayName;
			Status = m_Info.Status;
			IsMuted = m_Info.AudioMute;
			IsHost = m_Info.IsHost;
			IsSelf = m_Info.IsSelf;
			HandRaised = m_Info.HandRaised;

			if (EndTime != null && m_Info.Status == eParticipantStatus.Disconnected)
				EndTime = IcdEnvironment.GetUtcTime();

			if (IsSelf)
				SupportedParticipantFeatures |= eParticipantFeatures.RaiseLowerHand;
			else
				SupportedParticipantFeatures &= ~eParticipantFeatures.RaiseLowerHand;
		}

		#endregion

		#region Conference Component Callbacks

		private void Subscribe(ConferenceComponent conferenceComponent)
		{
			conferenceComponent.OnWebexParticipantsListSearchResult += ConferenceComponentOnWebexParticipantsListSearchResult;
			conferenceComponent.OnCallRecordingStatusChanged += ConferenceComponentOnCallRecordingStatusChanged;

			conferenceComponent.Codec.RegisterParserCallback(ConferenceCallCapabilitiesRecordStart,
			                                                 CiscoCodecDevice.XSTATUS_ELEMENT, "Conference", "Call",
			                                                 m_CallId.ToString(), "Capabilities",
			                                                 "Recording", "Start");
		}

		private void Unsubscribe(ConferenceComponent conferenceComponent)
		{
			conferenceComponent.OnWebexParticipantsListSearchResult -= ConferenceComponentOnWebexParticipantsListSearchResult;
			conferenceComponent.OnCallRecordingStatusChanged -= ConferenceComponentOnCallRecordingStatusChanged;

			conferenceComponent.Codec.UnregisterParserCallback(ConferenceCallCapabilitiesRecordStart,
			                                                   CiscoCodecDevice.XSTATUS_ELEMENT, "Conference", "Call",
			                                                   m_CallId.ToString(), "Capabilities",
			                                                   "Recording", "Start");
		}

		private void ConferenceComponentOnWebexParticipantsListSearchResult(object sender, GenericEventArgs<IEnumerable<WebexParticipantInfo>> args)
		{
			if (args.Data.Any(info => info.ParticipantId == m_Info.ParticipantId))
				UpdateInfo(args.Data.First(info => info.ParticipantId == m_Info.ParticipantId));
		}

		private void ConferenceComponentOnCallRecordingStatusChanged(object sender, GenericEventArgs<eCallRecordingStatus> args)
		{
			IsRecording = IsSelf && args.Data != eCallRecordingStatus.None;
			m_RecordingStatus = args.Data;
		}

		private void ConferenceCallCapabilitiesRecordStart(CiscoCodecDevice codec, string resultid, string xml)
		{
			CanRecord = IsSelf && XmlUtils.GetInnerXml(xml) == "Available";
		}

		#endregion
	}
}