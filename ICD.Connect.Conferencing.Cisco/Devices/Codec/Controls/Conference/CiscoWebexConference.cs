﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Conference;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference
{
	public sealed class CiscoWebexConference : AbstractConference<CiscoWebexParticipant>, ICiscoConference
	{
		#region Private Members

		private const int PARTICIPANT_SEARCH_LIMIT = 25;

		private CallStatus m_CallStatus;

		private readonly ConferenceComponent m_ConferenceComponent;
		private readonly DialingComponent m_DialingComponent;

		private readonly SafeCriticalSection m_ParticipantsSection;
		private readonly Dictionary<CiscoWebexParticipant, WebexParticipantInfo> m_ParticipantsToInfos;

		#endregion

		#region Properties

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="conferenceComponent"></param>
		/// <param name="dialingComponent"></param>
		/// <param name="callStatus"></param>
		public CiscoWebexConference([NotNull] ConferenceComponent conferenceComponent, DialingComponent dialingComponent, CallStatus callStatus)
		{
			if (conferenceComponent == null)
				throw new ArgumentNullException("conferenceComponent");

			if (dialingComponent == null)
				throw new ArgumentNullException("dialingComponent");

			m_ConferenceComponent = conferenceComponent;
			m_DialingComponent = dialingComponent;
			UpdateCallStatus(callStatus);

			Subscribe(m_ConferenceComponent);

			m_ParticipantsSection = new SafeCriticalSection();
			m_ParticipantsToInfos = new Dictionary<CiscoWebexParticipant, WebexParticipantInfo>();

			SupportedConferenceFeatures = eConferenceFeatures.LeaveConference |
			                              eConferenceFeatures.EndConference;
		}

		#endregion

		#region Methods

		public void UpdateCallStatus(CallStatus callStatus)
		{
			if (callStatus == null)
				return;

			m_CallStatus = callStatus;

			Name = m_CallStatus.Name;
			Number = m_CallStatus.Number;
			Direction = m_CallStatus.Direction;
			AnswerState = m_CallStatus.AnswerState;
			Status = m_CallStatus.Status.ToConferenceStatus();
		}

		public override void LeaveConference()
		{
			var self = GetParticipants().FirstOrDefault(p => p.IsSelf);
			if (self == null || !self.IsHost)
			{
				// Hangup if we are not the host.
				m_DialingComponent.Hangup(m_CallStatus);
				return;
			}

			// Transfer host and leave if we are the host.
			m_ConferenceComponent.TransferHostAndLeave(m_CallStatus.CallId);
		}

		public override void EndConference()
		{
			m_DialingComponent.Hangup(m_CallStatus);
		}

		/// <summary>
		/// Holds the conference
		/// </summary>
		public override void Hold()
		{
			m_DialingComponent.Hold(m_CallStatus);
		}

		/// <summary>
		/// Resumes the conference
		/// </summary>
		public override void Resume()
		{
			m_DialingComponent.Resume(m_CallStatus);
		}

		/// <summary>
		/// Sends DTMF to the participant.
		/// </summary>
		/// <param name="data"></param>
		public override void SendDtmf(string data)
		{
			m_DialingComponent.SendDtmf(m_CallStatus, data);
		}

		public override void StartRecordingConference()
		{
			m_ConferenceComponent.RecordingStart(m_CallStatus.CallId);
		}

		public override void StopRecordingConference()
		{
			m_ConferenceComponent.RecordingStop(m_CallStatus.CallId);
		}

		public override void PauseRecordingConference()
		{
			if (RecordingStatus == eConferenceRecordingStatus.Paused)
				m_ConferenceComponent.RecordingResume(m_CallStatus.CallId);
			else
				m_ConferenceComponent.RecordingPause(m_CallStatus.CallId);
		}

		#endregion

		#region Private Methods

		protected override void DisposeFinal()
		{
			Unsubscribe(m_ConferenceComponent);

			base.DisposeFinal();
		}

		/// <summary>
		/// Override to handle the conference status changing
		/// </summary>
		/// <param name="status"></param>
		protected override void HandleStatusChanged(eConferenceStatus status)
		{
			base.HandleStatusChanged(status);

			if (status == eConferenceStatus.Connected)
				m_ConferenceComponent.ParticipantListSearch(m_CallStatus.CallId, PARTICIPANT_SEARCH_LIMIT, null, null);
		}

		#endregion

		#region Conference Component Callbacks

		private void Subscribe(ConferenceComponent conferenceComponent)
		{
			conferenceComponent.OnWebexParticipantListUpdated += ConferenceComponentOnWebexParticipantListUpdated;
			conferenceComponent.OnWebexParticipantsListSearchResult += ConferenceComponentOnWebexParticipantsListSearchResult;
			conferenceComponent.OnCallRecordingStatusChanged += ConferenceComponentOnCallRecordingStatusChanged;

			conferenceComponent.Codec.RegisterParserCallback(ConferenceCallCapabilitiesRecordStart,
			                                                 CiscoCodecDevice.XSTATUS_ELEMENT, "Conference", "Call",
			                                                 m_CallStatus.CallId.ToString(), "Capabilities",
			                                                 "Recording", "Start");
		}

		private void Unsubscribe(ConferenceComponent conferenceComponent)
		{
			conferenceComponent.OnWebexParticipantListUpdated -= ConferenceComponentOnWebexParticipantListUpdated;
			conferenceComponent.OnWebexParticipantsListSearchResult -= ConferenceComponentOnWebexParticipantsListSearchResult;
			conferenceComponent.OnCallRecordingStatusChanged -= ConferenceComponentOnCallRecordingStatusChanged;

			conferenceComponent.Codec.UnregisterParserCallback(ConferenceCallCapabilitiesRecordStart,
			                                                   CiscoCodecDevice.XSTATUS_ELEMENT, "Conference", "Call",
			                                                   m_CallStatus.CallId.ToString(), "Capabilities",
			                                                   "Recording", "Start");
		}

		private void ConferenceComponentOnWebexParticipantListUpdated(object sender, GenericEventArgs<WebexParticipantInfo> args)
		{
			if (args.Data.CallId != m_CallStatus.CallId)
				return;

			m_ConferenceComponent.ParticipantListSearch(m_CallStatus.CallId, PARTICIPANT_SEARCH_LIMIT, null, null);
		}

		private void ConferenceComponentOnWebexParticipantsListSearchResult(object sender, GenericEventArgs<IEnumerable<WebexParticipantInfo>> args)
		{
			var newInfos = new List<WebexParticipantInfo>();

			// Search for any new participants
			m_ParticipantsSection.Execute(() => newInfos.AddRange(args.Data.Where(info => m_ParticipantsToInfos.Values.All(k => k.ParticipantId != info.ParticipantId))));

			foreach (WebexParticipantInfo info in newInfos)
			{
				var participant = new CiscoWebexParticipant(info, m_CallStatus.CallId, m_ConferenceComponent);
				AddParticipant(participant);
				Subscribe(participant);

				m_ParticipantsSection.Execute(() => m_ParticipantsToInfos.Add(participant, info));
			}
		}

		private void ConferenceComponentOnCallRecordingStatusChanged(object sender, GenericEventArgs<eCallRecordingStatus> args)
		{
			switch (args.Data)
			{
				case eCallRecordingStatus.None:
					RecordingStatus = eConferenceRecordingStatus.Stopped;
					break;
				case eCallRecordingStatus.Recording:
					RecordingStatus = eConferenceRecordingStatus.Recording;
					break;
				case eCallRecordingStatus.Paused:
					RecordingStatus = eConferenceRecordingStatus.Paused;
					break;
				default:
					RecordingStatus = eConferenceRecordingStatus.Unknown;
					break;
			}
		}

		private void ConferenceCallCapabilitiesRecordStart(CiscoCodecDevice codec, string resultid, string xml)
		{
			SupportedConferenceFeatures =
				SupportedConferenceFeatures.SetFlags(eConferenceFeatures.StartRecording |
				                                     eConferenceFeatures.StopRecording |
				                                     eConferenceFeatures.PauseRecording,
				                                     XmlUtils.GetInnerXml(xml) == "Available");
		}

		private void Subscribe(CiscoWebexParticipant participant)
		{
			if (participant == null)
				return;

			participant.OnStatusChanged += ParticipantOnStatusChanged;
		}

		private void Unsubscribe(CiscoWebexParticipant participant)
		{
			if (participant == null)
				return;

			participant.OnStatusChanged -= ParticipantOnStatusChanged;
		}

		private void ParticipantOnStatusChanged(object sender, ParticipantStatusEventArgs args)
		{
			var participant = sender as CiscoWebexParticipant;
			if (participant == null)
				return;

			if (args.Data != eParticipantStatus.Disconnected)
				return;

			Unsubscribe(participant);
			RemoveParticipant(participant);

			m_ParticipantsSection.Execute(() => m_ParticipantsToInfos.Remove(participant));
		}

		#endregion
	}
}