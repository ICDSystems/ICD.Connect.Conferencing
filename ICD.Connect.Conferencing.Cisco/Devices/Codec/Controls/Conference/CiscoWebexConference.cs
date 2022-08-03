using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Conference;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference
{
	public sealed class CiscoWebexConference : AbstractConferenceBase<CiscoWebexParticipant>, ICiscoConference
	{
		#region Private Members

		private const int PARTICIPANT_SEARCH_LIMIT = 25;

		private const long PARTICIPANT_LIST_UPDATE_INTERVAL = 30 * 1000;

		private const long AUTHENTICATION_FAILED_TIMEOUT = 8 * 1000;

		private const string AUTHENTICATION_FAILED_MESSAGE = "Could Not Join, Please Try Again";

		private CallStatus m_CallStatus;

		private readonly ConferenceComponent m_ConferenceComponent;
		private readonly DialingComponent m_DialingComponent;

		private readonly SafeTimer m_ParticipantUpdateTimer;

		private readonly SafeCriticalSection m_ParticipantsSection;

		private readonly SafeTimer m_AuthenticationFailedTimer;

		private bool m_IsHostOrCoHost;

		/// <summary>
		/// Participants
		/// Key is webex participant id
		/// </summary>
		private readonly Dictionary<string, CiscoWebexParticipant> m_Participants;

		private CiscoWebexParticipant m_SelfParticipant;

		/// <summary>
		/// This is set to true when the start time is updated from the call duration
		/// Once it's updated once, don't update from the duration any more
		/// Because start time tends to drift.
		/// </summary>
		private bool m_StartTimeSetFromDuration;

		#endregion

		#region Events

		public override event EventHandler<ParticipantEventArgs> OnParticipantAdded;
		public override event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		#endregion

		#region Properties

		public CallStatus CallStatus{ get { return m_CallStatus; }}

		public CiscoWebexParticipant SelfParticipant
		{
			get { return m_SelfParticipant; }
			private set
			{
				if (m_SelfParticipant == value)
					return;

				UnsubscribeSelfParticipant(m_SelfParticipant);
				m_SelfParticipant = value;
				SubscribeSelfParticipant(m_SelfParticipant);

				UpdateIsHostOrCoHost();
			}
		}

		public bool IsHostOrCoHost
		{
			get { return m_IsHostOrCoHost; }
			private set
			{
				if (m_IsHostOrCoHost == value)
					return;

				m_IsHostOrCoHost = value;

				SetParticipantsCanKickAndMute(value);
			}
		}

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
			m_ParticipantsSection = new SafeCriticalSection();
			m_Participants = new Dictionary<string, CiscoWebexParticipant>();
			m_ParticipantUpdateTimer = SafeTimer.Stopped(ParticipantUpdateTimerCallback);
			m_AuthenticationFailedTimer = SafeTimer.Stopped(AuthenticationFailedCallback);
			
			// Set initial start time - gets updated later by the duration time
			StartTime = IcdEnvironment.GetUtcTime();

			UpdateCallStatus(callStatus);

			Subscribe(m_ConferenceComponent);

			

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
			CallType = m_CallStatus.CiscoCallType.ToCallType();
			// Only update the start time if it hasn't already been updated from duration - drifts
			if (!m_StartTimeSetFromDuration && m_CallStatus.Duration != 0 &&
			    m_CallStatus.Status == eCiscoCallStatus.Connected)
			{
				StartTime = IcdEnvironment.GetUtcTime().AddSeconds(m_CallStatus.Duration * -1);
				m_StartTimeSetFromDuration = true;
			}
		}

		/// <summary>
		/// Gets the participants in this conference.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<CiscoWebexParticipant> GetParticipants()
		{
			return m_ParticipantsSection.Execute(() => m_Participants.Values.ToArray(m_Participants.Count));
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

		private void AddParticipant([NotNull] CiscoWebexParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			Subscribe(participant);
			m_ParticipantsSection.Execute(() => m_Participants.Add(participant.WebexParticipantId, participant));
			OnParticipantAdded.Raise(this, participant);

			if (participant.IsSelf)
				SelfParticipant = participant;
		}

		private void RemoveParticipant([NotNull] CiscoWebexParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			Unsubscribe(participant);
			m_ParticipantsSection.Execute(() => m_Participants.Remove(participant.WebexParticipantId));
			OnParticipantRemoved.Raise(this, participant);
		}

		/// <summary>
		/// Override to handle the conference status changing
		/// </summary>
		/// <param name="status"></param>
		protected override void HandleStatusChanged(eConferenceStatus status)
		{
			base.HandleStatusChanged(status);

			if (status == eConferenceStatus.Connected)
			{
				// Set start time
				StartTime = IcdEnvironment.GetUtcTime();

				// Pull current participant list and start update timer
				ParticipantListUpdate();
				m_ParticipantUpdateTimer.Reset(PARTICIPANT_LIST_UPDATE_INTERVAL, PARTICIPANT_LIST_UPDATE_INTERVAL);
			}
			else
			{
				//If Disconnected, set end time
				if (status == eConferenceStatus.Disconnected)
					EndTime = IcdEnvironment.GetUtcTime();

				// Stop updating when not connected
				m_ParticipantUpdateTimer.Stop();
			}
		}

		private void ParticipantUpdateTimerCallback()
		{
			if (Status == eConferenceStatus.Connected)
				ParticipantListUpdate();
		}

		private void ParticipantListUpdate()
		{
			m_ConferenceComponent.ParticipantListSearch(m_CallStatus.CallId, PARTICIPANT_SEARCH_LIMIT, null, null);
		}
		
		private void SetParticipantsCanKickAndMute(bool value)
		{
			CiscoWebexParticipant[] participants = null;

			m_ParticipantsSection.Execute(() => participants = m_Participants.Values.ToArray(m_Participants.Count));

			foreach (var participant in participants)
				participant.SupportsHostParticipantPermissions(value);
		}

		private void UpdateIsHostOrCoHost()
		{
			IsHostOrCoHost = SelfParticipant != null && (SelfParticipant.IsHost || SelfParticipant.IsCoHost);
		}

		protected override void DisposeFinal()
		{
			Unsubscribe(m_ConferenceComponent);

			m_ParticipantUpdateTimer.Stop();
			m_ParticipantUpdateTimer.Dispose();

			CiscoWebexParticipant[] participants;

			m_ParticipantsSection.Enter();
			try
			{
				participants = m_Participants.Values.ToArray(m_Participants.Count);
				m_Participants.Clear();
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}

			//todo: Fire OnParticipantRemoved event??

			foreach (CiscoWebexParticipant participant in participants)
			{
				Unsubscribe(participant);
				participant.Dispose();
			}

			base.DisposeFinal();
		}

		private void UpdateAuthenticationOptions(eAuthenticationRequest authenticationRequest)
		{
			m_AuthenticationFailedTimer.Stop();
			switch (authenticationRequest)
			{
				case eAuthenticationRequest.Unknown:
				case eAuthenticationRequest.None:
					AuthenticationOptions = null;
					break;
				case eAuthenticationRequest.GuestPin:
					AuthenticationOptions = new ConferenceAuthenticationOptions(eConferenceAuthenticationState.Required,
						false, GetGuestPinAuthenticationMethods());
					break;
				case eAuthenticationRequest.PanelistPin:
					AuthenticationOptions = new ConferenceAuthenticationOptions(eConferenceAuthenticationState.Required,
						false, GetPanelistPinAuthenticationMethods());
					break;
				case eAuthenticationRequest.HostPinOrGuest:
					AuthenticationOptions = new ConferenceAuthenticationOptions(eConferenceAuthenticationState.Required,
						false,  GetHostPinOrGuestAuthenticationMethods());
					break;
				case eAuthenticationRequest.HostPinOrGuestPin:
					AuthenticationOptions = new ConferenceAuthenticationOptions(eConferenceAuthenticationState.Required,
						false, GetHostPinOrGuestPinAuthenticationMethods());
					break;
				case eAuthenticationRequest.AnyHostPinOrGuestPin:
					AuthenticationOptions = new ConferenceAuthenticationOptions(eConferenceAuthenticationState.Required,
						false, GetAnyHostPinOrGuestPinAuthenticationMethods());
					break;
				
				default:
					throw new ArgumentOutOfRangeException("authenticationRequest");

			}
		}

		private IEnumerable<ConferenceAuthenticationMethod> GetHostPinOrGuestAuthenticationMethods()
		{
			yield return GetGuestAuthenticationMethod();
			yield return GetHostPinAuthenticationMethod();
		}

		private IEnumerable<ConferenceAuthenticationMethod> GetGuestPinAuthenticationMethods()
		{
			yield return GetGuestPinAuthenticationMethod();
		}

		private IEnumerable<ConferenceAuthenticationMethod> GetPanelistPinAuthenticationMethods()
		{
			yield return new ConferenceAuthenticationMethod("Panelist", "Enter Panelist Password", eCodeRequirement.Required, AuthenticatePanelistPin);
		}

		private IEnumerable<ConferenceAuthenticationMethod> GetHostPinOrGuestPinAuthenticationMethods()
		{
			yield return GetGuestPinAuthenticationMethod();
			yield return GetHostPinAuthenticationMethod();
		}

		private IEnumerable<ConferenceAuthenticationMethod> GetAnyHostPinOrGuestPinAuthenticationMethods()
		{
			yield return GetGuestPinAuthenticationMethod();
			yield return GetHostPinAuthenticationMethod();
			yield return new ConferenceAuthenticationMethod("CoHost", "Enter CoHost Password", eCodeRequirement.Required, AuthenticateCoHostPin);
		}

		private ConferenceAuthenticationMethod GetHostPinAuthenticationMethod()
		{
			return new ConferenceAuthenticationMethod("Host", "Enter Host Password", eCodeRequirement.Required, AuthenticateHostPin);
		}

		private ConferenceAuthenticationMethod GetGuestPinAuthenticationMethod()
		{
			return new ConferenceAuthenticationMethod("Guest", "Enter Guest Password", eCodeRequirement.Required, AuthenticateGuestPin);
		}
		
		private ConferenceAuthenticationMethod GetGuestAuthenticationMethod()
		{
			return new ConferenceAuthenticationMethod("Guest", "Password not required for guest", eCodeRequirement.NotSupported, AuthenticateGuest);
		}
		
		/// <summary>
		/// Calls when the timer expires to inform the user that the password did not work
		/// Since we get no notification from the endpoint
		/// </summary>
		private void AuthenticationFailedCallback()
		{
			if (AuthenticationOptions != null)
				AuthenticationOptions.RaisePasswordRejected(AUTHENTICATION_FAILED_MESSAGE);
		}


		private void AuthenticateGuestPin(string pin)
		{
			m_AuthenticationFailedTimer.Reset(AUTHENTICATION_FAILED_TIMEOUT);
			m_DialingComponent.AuthenticateGuestPin(m_CallStatus, pin);
		}

		private void AuthenticatePanelistPin(string pin)
		{
			m_AuthenticationFailedTimer.Reset(AUTHENTICATION_FAILED_TIMEOUT);
			m_DialingComponent.AuthenticatePanelistPin(m_CallStatus, pin);	
		}

		private void AuthenticateHostPin(string pin)
		{
			m_AuthenticationFailedTimer.Reset(AUTHENTICATION_FAILED_TIMEOUT);
			m_DialingComponent.AuthenticateHostPin(m_CallStatus, pin);
		}

		private void AuthenticateCoHostPin(string pin)
		{
			m_AuthenticationFailedTimer.Reset(AUTHENTICATION_FAILED_TIMEOUT);
			m_DialingComponent.AuthenticateCoHostPin(m_CallStatus, pin);
		}

		
		private void AuthenticateGuest(string pin)
		{
			// pin not used for this authentication method
			m_DialingComponent.AuthenticateGuest(m_CallStatus);
		}

		#endregion

		#region Participant Callbacks

		private void Subscribe(CiscoWebexParticipant participant)
		{
			if (participant == null)
				return;

			participant.OnStatusChanged += ParticipantOnStatusChanged;
			participant.OnIsSelfChanged += ParticipantOnIsSelfChanged;
		}

		private void Unsubscribe(CiscoWebexParticipant participant)
		{
			if (participant == null)
				return;

			participant.OnStatusChanged -= ParticipantOnStatusChanged;
			participant.OnIsSelfChanged -= ParticipantOnIsSelfChanged;
		}

		private void ParticipantOnStatusChanged(object sender, ParticipantStatusEventArgs args)
		{
			var participant = sender as CiscoWebexParticipant;
			if (participant == null)
				return;

			if (args.Data == eParticipantStatus.Disconnected)           
				RemoveParticipant(participant);
		}

		private void ParticipantOnIsSelfChanged(object sender, BoolEventArgs args)
		{
			if (!args.Data)
				return;

			CiscoWebexParticipant participant = sender as CiscoWebexParticipant;
			SelfParticipant = participant;
		}

		private void ParticipantAdmit(string participantId)
		{
			m_ConferenceComponent.ParticipantAdmit(m_CallStatus.CallId, participantId);
		}

		private void ParticipantKick(string participantId)
		{
			m_ConferenceComponent.ParticipantDisconnect(m_CallStatus.CallId, participantId);
		}

		private void SetParticipantMute(string participantId, bool state)
		{
			m_ConferenceComponent.ParticipantMute(state, m_CallStatus.CallId, participantId);
		}

		private void SetParticipantHandPosition(bool state)
		{
			if (state)
				m_ConferenceComponent.RaiseHand(m_CallStatus.CallId);
			else
				m_ConferenceComponent.LowerHand(m_CallStatus.CallId);
		}

		#endregion

		#region Self Participant Callbacks

		private void SubscribeSelfParticipant(CiscoWebexParticipant participant)
		{
			if (participant == null)
				return;

			participant.OnIsHostChanged += SelfParticipantOnIsHostChanged;
			participant.OnIsCoHostChanged += SelfParticipantOnIsCoHostChanged;
		}

		private void UnsubscribeSelfParticipant(CiscoWebexParticipant participant)
		{
			if (participant == null)
				return;

			participant.OnIsHostChanged -= SelfParticipantOnIsHostChanged;
			participant.OnIsCoHostChanged -= SelfParticipantOnIsCoHostChanged;
		}

		private void SelfParticipantOnIsCoHostChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateIsHostOrCoHost();
		}

		private void SelfParticipantOnIsHostChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateIsHostOrCoHost();
		}

		#endregion

		#region Conference Component Callbacks

		private void Subscribe(ConferenceComponent conferenceComponent)
		{
			conferenceComponent.OnWebexParticipantListUpdated += ConferenceComponentOnWebexParticipantListUpdated;
			conferenceComponent.OnWebexParticipantsListSearchResult +=
				ConferenceComponentOnWebexParticipantsListSearchResult;
			conferenceComponent.OnCallRecordingStatusChanged += ConferenceComponentOnCallRecordingStatusChanged;

			conferenceComponent.Codec.RegisterParserCallback(ConferenceCallCapabilitiesRecordStart,
				CiscoCodecDevice.XSTATUS_ELEMENT, "Conference", "Call", "Capabilities",
				"Recording", "Start");
			conferenceComponent.Codec.RegisterParserCallback(ConferenceCallAuthenticationRequest,
				CiscoCodecDevice.XSTATUS_ELEMENT, "Conference", "Call", "AuthenticationRequest");
		}

		private void Unsubscribe(ConferenceComponent conferenceComponent)
		{
			conferenceComponent.OnWebexParticipantListUpdated -= ConferenceComponentOnWebexParticipantListUpdated;
			conferenceComponent.OnWebexParticipantsListSearchResult -=
				ConferenceComponentOnWebexParticipantsListSearchResult;
			conferenceComponent.OnCallRecordingStatusChanged -= ConferenceComponentOnCallRecordingStatusChanged;

			conferenceComponent.Codec.UnregisterParserCallback(ConferenceCallCapabilitiesRecordStart,
				CiscoCodecDevice.XSTATUS_ELEMENT, "Conference", "Call",
				m_CallStatus.CallId.ToString(), "Capabilities",
				"Recording", "Start");
			conferenceComponent.Codec.UnregisterParserCallback(ConferenceCallAuthenticationRequest,
				CiscoCodecDevice.XSTATUS_ELEMENT, "Conference", "call", m_CallStatus.CallId.ToString(),
				"AuthenticationRequest");
		}

		private void ConferenceComponentOnWebexParticipantListUpdated(object sender, GenericEventArgs<WebexParticipantInfo> args)
		{
			if (args.Data.CallId != m_CallStatus.CallId)
				return;
			
			CiscoWebexParticipant participant;

			m_ParticipantsSection.Enter();
			try
			{
				
				//Check if particpant exists
				if (!m_Participants.TryGetValue(args.Data.ParticipantId, out participant))
				{
					// Don't do anything if we're at the search limit
					if (m_Participants.Count >= PARTICIPANT_SEARCH_LIMIT)
						return;

					// Add new participant
					participant = new CiscoWebexParticipant(args.Data, () => ParticipantAdmit(args.Data.ParticipantId), () => ParticipantKick(args.Data.ParticipantId), state => SetParticipantMute(args.Data.ParticipantId, state), SetParticipantHandPosition, IsHostOrCoHost);
					AddParticipant(participant);
					return;
				}
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}

			// Update existing participant
			participant.UpdateInfo(args.Data);
		}

		private void ConferenceComponentOnWebexParticipantsListSearchResult(object sender, GenericEventArgs<WebexParticipantInfo[]> args)
		{
			// We don't update existing participants here, since they should get update by the OnWebexParticipantListUpdated event - this may end up being a poor choice (it was)

			var results = new Dictionary<string, WebexParticipantInfo>();
			// CallId isn't participant search results - this will be problematic if we ever have multiple webex calls possible
			results.AddRange(args.Data, i => i.ParticipantId);

			// Caculate old participants to remove
			var removedParticipants = new List<CiscoWebexParticipant>();
			m_ParticipantsSection.Execute(() =>
										  removedParticipants.AddRange(
										  m_Participants.Values.Where(participant => !results.ContainsKey(participant.WebexParticipantId))));

			// Loop over received participants and update them or add new
			foreach (WebexParticipantInfo info in results.Values)
			{
				CiscoWebexParticipant participant = null;
				string participantId = info.ParticipantId;
				if (m_ParticipantsSection.Execute(() => m_Participants.TryGetValue(participantId, out participant)))
				{
					// Participant Exists, update it
					participant.UpdateInfo(info);
				}
				else
				{
					// Create a new participant and add it
					participant = new CiscoWebexParticipant(info, () => ParticipantAdmit(participantId),
					                                        () => ParticipantKick(participantId),
					                                        state => SetParticipantMute(participantId, state),
					                                        SetParticipantHandPosition, IsHostOrCoHost);
					AddParticipant(participant);
				}
			}

			// Remove old participants
			removedParticipants.ForEach(RemoveParticipant);
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

		private void ConferenceCallAuthenticationRequest(CiscoCodecDevice codec, string resultId, string xml)
		{
			UpdateAuthenticationOptions(EnumUtils.Parse<eAuthenticationRequest>(XmlUtils.GetInnerXml(xml), false));
		}

		#endregion
	}
}