using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Conferencing.ConferencePoints;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.ConferenceManagers
{
	public sealed class ConferenceManagerDialers
	{
		/// <summary>
		/// Raised when the active conference changes.
		/// </summary>
		public event EventHandler<ConferenceEventArgs> OnConferenceAdded;

		/// <summary>
		/// Raised when the active conference ends.
		/// </summary>
		public event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		/// <summary>
		/// Called when the active conference status changes.
		/// </summary>
		public event EventHandler<ConferenceStatusEventArgs> OnActiveConferenceStatusChanged;

		/// <summary>
		/// Called when an active participant status changes.
		/// </summary>
		public event EventHandler<ParticipantStatusEventArgs> OnActiveParticipantStatusChanged;

		/// <summary>
		/// Raises when the in call state changes.
		/// </summary>
		public event EventHandler<InCallEventArgs> OnInCallChanged;

		/// <summary>
		/// Raises when the conference adds or removes a participant.
		/// </summary>
		public event EventHandler<ConferenceParticipantAddedOrRemovedEventArgs> OnConferenceParticipantAddedOrRemoved;

		/// <summary>
		/// Called when an incoming call is added by a conference control.
		/// </summary>
		public event EventHandler<ConferenceControlIncomingCallEventArgs> OnIncomingCallAdded;

		/// <summary>
		/// Called when an incoming call is removed by a conference control.
		/// </summary>
		public event EventHandler<ConferenceControlIncomingCallEventArgs> OnIncomingCallRemoved;

		/// <summary>
		/// Raised when the recording status of a conference changes.
		/// </summary>
		public event EventHandler<GenericEventArgs<IConference>> OnConferenceRecordStateChanged;

		/// <summary>
		/// Raised when a participant's supported features changes.
		/// </summary>
		public event EventHandler<GenericEventArgs<IParticipant>> OnParticipantSupportedFeaturesChanged;

		/// <summary>
		/// Raised when an active participant's virtual hand raised state changes.
		/// </summary>
		public event EventHandler OnParticipantHandRaiseStateChanged;

		private readonly IConferenceManager m_ConferenceManager;

		private readonly IcdHashSet<IConference> m_Conferences;
		private readonly BiDictionary<IConferencePoint, IConferenceDeviceControl> m_Points;
		private readonly IcdHashSet<IConferenceDeviceControl> m_FeedbackProviders;

		private readonly SafeCriticalSection m_ConferencesSection;
		private readonly SafeCriticalSection m_PointsSection;
		private readonly SafeCriticalSection m_FeedbackProviderSection;

		private eInCall m_IsInCall;

		#region Properties

		/// <summary>
		/// Gets the active conferences.
		/// </summary>
		public IEnumerable<IConference> ActiveConferences { get { return m_Conferences.Where(c => c.IsActive()); } }

		/// <summary>
		/// Returns true if actively in a call.
		/// </summary>
		public eInCall IsInCall
		{
			get { return m_IsInCall; }
			private set
			{
				if (value == m_IsInCall)
					return;

				m_IsInCall = value;

				OnInCallChanged.Raise(this, new InCallEventArgs(m_IsInCall));
			}
		}

		#endregion

		/// <summary>
		/// Constructors.
		/// </summary>
		/// <param name="conferenceManager"></param>
		public ConferenceManagerDialers([NotNull] IConferenceManager conferenceManager)
		{
			if (conferenceManager == null)
				throw new ArgumentNullException("conferenceManager");

			m_ConferenceManager = conferenceManager;

			m_Conferences = new IcdHashSet<IConference>();
			m_Points = new BiDictionary<IConferencePoint, IConferenceDeviceControl>();
			m_FeedbackProviders = new IcdHashSet<IConferenceDeviceControl>();

			m_ConferencesSection = new SafeCriticalSection();
			m_PointsSection = new SafeCriticalSection();
			m_FeedbackProviderSection = new SafeCriticalSection();

			Subscribe(m_ConferenceManager);
		}

		#region Methods

		/// <summary>
		/// Deregisters all of the conference controls.
		/// </summary>
		public void Clear()
		{
			m_PointsSection.Enter();

			try
			{
				foreach (IConferencePoint conferencePoint in m_Points.Keys.ToArray(m_Points.Count))
					DeregisterConferencePoint(conferencePoint);
			}
			finally
			{
				m_PointsSection.Leave();
			}

			m_FeedbackProviderSection.Enter();

			try
			{
				foreach (IConferenceDeviceControl provider in m_FeedbackProviders.ToArray(m_FeedbackProviders.Count))
					DeregisterFeedbackDialingProvider(provider);
			}
			finally
			{
				m_FeedbackProviderSection.Leave();
			}

			m_ConferencesSection.Execute(() => RemoveConferences(m_Conferences.ToArray()));
		}

		/// <summary>
		/// Gets the registered conference providers.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<IConferenceDeviceControl> GetDialingProviders()
		{
			return m_PointsSection.Execute(() => m_Points.Values.ToArray(m_Points.Count));
		}

		/// <summary>
		/// Gets the registered conference components.
		/// </summary>
		/// <param name="callType"></param>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<IConferenceDeviceControl> GetDialingProviders(eCallType callType)
		{
			m_PointsSection.Enter();

			try
			{
				return m_Points.Where(kvp => kvp.Key.Type.HasFlags(callType))
				               .Select(kvp => kvp.Value)
				               .ToArray();
			}
			finally
			{
				m_PointsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the best dialer for the given dial context.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		[CanBeNull]
		public IConferenceDeviceControl GetBestDialer([NotNull] IDialContext dialContext)
		{
			if (dialContext == null)
				throw new ArgumentNullException("dialContext");

			m_PointsSection.Enter();

			try
			{
				IGrouping<eDialContextSupport, IConferenceDeviceControl> bestGroup =
					m_Points.Where(kvp => kvp.Key.Type.HasFlag(dialContext.CallType) &&
					                      kvp.Value.Supports.HasFlags(dialContext.CallType))
					        .Select(kvp => kvp.Value)
					        .GroupBy(c => c.CanDial(dialContext))
					        .Where(kvp => kvp.Key != eDialContextSupport.Unsupported)
					        .OrderByDescending(g => g.Key)
					        .FirstOrDefault();

				return bestGroup == null ? null : bestGroup.FirstOrDefault();
			}
			finally
			{
				m_PointsSection.Leave();
			}
		}

		/// <summary>
		/// Gets the registered feedback conference providers.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConferenceDeviceControl> GetFeedbackDialingProviders()
		{
			return m_FeedbackProviderSection.Execute(() => m_FeedbackProviders.ToArray());
		}

		/// <summary>
		/// Gets the registered conference providers that are currently in an active conference.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConferenceDeviceControl> GetActiveDialers()
		{
			return GetDialingProviders().Concat(GetFeedbackDialingProviders())
			                            .Where(d => d.GetActiveConferences().Any());
		}

		/// <summary>
		/// Registers the dialing provider at the given conference point.
		/// </summary>
		/// <param name="conferencePoint"></param>
		public void RegisterConferencePoint([NotNull] IConferencePoint conferencePoint)
		{
			if (conferencePoint == null)
				throw new ArgumentNullException("conferencePoint");

			if (conferencePoint.Control == null)
				throw new ArgumentException("Conference point does not have a conference control");

			m_PointsSection.Enter();

			try
			{
				if (m_Points.ContainsKey(conferencePoint))
					return;

				Unsubscribe(conferencePoint.Control);
				m_Points.Add(conferencePoint, conferencePoint.Control);
				Subscribe(conferencePoint.Control);

				UpdateProvider(conferencePoint.Control);
			}
			finally
			{
				m_PointsSection.Leave();
			}
		}

		/// <summary>
		/// Deregisters the conference point.
		/// </summary>
		/// <param name="conferencePoint"></param>
		/// <returns></returns>
		public void DeregisterConferencePoint(IConferencePoint conferencePoint)
		{
			if (conferencePoint == null)
				throw new ArgumentNullException("conferencePoint");

			m_PointsSection.Enter();

			try
			{
				IConferenceDeviceControl conferenceControl;
				if (!m_Points.TryGetValue(conferencePoint, out conferenceControl))
					return;

				Unsubscribe(conferenceControl);

				m_Points.RemoveKey(conferencePoint);
			}
			finally
			{
				m_PointsSection.Leave();
			}
		}

		/// <summary>
		/// Registers the conference component, for feedback only.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		public bool RegisterFeedbackDialingProvider([NotNull] IConferenceDeviceControl conferenceControl)
		{
			if (conferenceControl == null)
				throw new ArgumentNullException("conferenceControl");

			m_FeedbackProviderSection.Enter();

			try
			{
				if (m_FeedbackProviders.Contains(conferenceControl))
					return false;

				m_FeedbackProviders.Add(conferenceControl);
				Subscribe(conferenceControl);

				UpdateFeedbackProvider(conferenceControl);
			}
			finally
			{
				m_FeedbackProviderSection.Leave();
			}

			AddConferences(conferenceControl.GetConferences());

			return true;
		}

		/// <summary>
		/// Deregisters the conference component from the feedback only list.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		public bool DeregisterFeedbackDialingProvider(IConferenceDeviceControl conferenceControl)
		{
			m_FeedbackProviderSection.Enter();

			try
			{
				if (!m_FeedbackProviders.Remove(conferenceControl))
					return false;

				Unsubscribe(conferenceControl);
			}
			finally
			{
				m_FeedbackProviderSection.Leave();
			}

			RemoveConferences(conferenceControl.GetConferences());

			return true;
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Adds the conferences to the conferences collection.
		/// </summary>
		/// <param name="conferences"></param>
		private void AddConferences([NotNull] IEnumerable<IConference> conferences)
		{
			if (conferences == null)
				throw new ArgumentNullException("conferences");

			foreach (IConference conference in conferences)
				AddConference(conference);
		}

		/// <summary>
		/// Adds the conference to the conferences collection.
		/// </summary>
		/// <param name="conference"></param>
		private void AddConference([NotNull] IConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			m_ConferencesSection.Enter();

			try
			{
				if (!m_Conferences.Add(conference))
					return;

				Subscribe(conference);
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			UpdateIsInCall();

			OnConferenceAdded.Raise(this, new ConferenceEventArgs(conference));
		}

		/// <summary>
		/// Removes the conferences from the conferences collection.
		/// </summary>
		/// <param name="conferences"></param>
		private void RemoveConferences([NotNull] IEnumerable<IConference> conferences)
		{
			if (conferences == null)
				throw new ArgumentNullException("conferences");

			foreach (IConference conference in conferences)
				RemoveConference(conference);
		}

		/// <summary>
		/// Removes the conference from the conferences collection.
		/// </summary>
		/// <param name="conference"></param>
		private void RemoveConference([NotNull] IConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			m_ConferencesSection.Enter();

			try
			{
				if (!m_Conferences.Remove(conference))
					return;

				Unsubscribe(conference);

				foreach (IParticipant participant in conference.GetParticipants())
					Unsubscribe(participant);
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			UpdateIsInCall();

			OnConferenceRemoved.Raise(this, new ConferenceEventArgs(conference));
		}

		/// <summary>
		/// Updates the conference providers to match the state of the conference manager.
		/// </summary>
		private void UpdateProviders()
		{
			foreach (IConferenceDeviceControl provider in GetDialingProviders())
				UpdateProvider(provider);

			foreach (IConferenceDeviceControl provider in GetFeedbackDialingProviders())
				UpdateFeedbackProvider(provider);
		}

		/// <summary>
		/// Updates the feedback conference provider to match the state of the conference manager.
		/// </summary>
		/// <param name="conferenceControl"></param>
		private void UpdateFeedbackProvider([NotNull] IConferenceDeviceControl conferenceControl)
		{
			if (conferenceControl == null)
				throw new ArgumentNullException("conferenceControl");

			if (!m_ConferenceManager.IsActive)
				return;

			bool privacyMute = m_ConferenceManager.PrivacyMuted;
			if (conferenceControl.PrivacyMuted != privacyMute && conferenceControl.SupportedConferenceControlFeatures.HasFlag(eConferenceControlFeatures.PrivacyMute))
				conferenceControl.SetPrivacyMute(privacyMute);
		}

		/// <summary>
		/// Updates the conference provider to match the state of the conference manager.
		/// </summary>
		/// <param name="conferenceControl"></param>
		private void UpdateProvider([NotNull] IConferenceDeviceControl conferenceControl)
		{
			if (conferenceControl == null)
				throw new ArgumentNullException("conferenceControl");

			if (!m_ConferenceManager.IsActive)
				return;

			// Enforce auto-answer
			if (conferenceControl.SupportedConferenceControlFeatures.HasFlag(eConferenceControlFeatures.AutoAnswer))
			{
				switch (m_ConferenceManager.EnforceAutoAnswer)
				{
					case eEnforceState.DoNotEnforce:
						break;
					case eEnforceState.EnforceOn:
						conferenceControl.SetAutoAnswer(true);
						break;
					case eEnforceState.EnforceOff:
						conferenceControl.SetAutoAnswer(false);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			// Enforce do-not-disturb
			if (conferenceControl.SupportedConferenceControlFeatures.HasFlag(eConferenceControlFeatures.DoNotDisturb))
			{
				switch (m_ConferenceManager.EnforceDoNotDisturb)
				{
					case eEnforceState.DoNotEnforce:
						break;
					case eEnforceState.EnforceOn:
						conferenceControl.SetDoNotDisturb(true);
						break;
					case eEnforceState.EnforceOff:
						conferenceControl.SetDoNotDisturb(false);
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			IConferencePoint point = m_PointsSection.Execute(() => m_Points.GetKey(conferenceControl));

			// Enforce privacy mute
			bool privacyMute = m_ConferenceManager.PrivacyMuted;
			if (point.PrivacyMuteMask.HasFlag(ePrivacyMuteFeedback.Set) && conferenceControl.PrivacyMuted != privacyMute && conferenceControl.SupportedConferenceControlFeatures.HasFlag(eConferenceControlFeatures.PrivacyMute))
				conferenceControl.SetPrivacyMute(privacyMute);

			// Enforce camera privacy mute
			bool cameraPrivacyMute = m_ConferenceManager.CameraPrivacyMuted;
			if (conferenceControl.CameraMute != cameraPrivacyMute && conferenceControl.SupportedConferenceControlFeatures.HasFlag(eConferenceControlFeatures.CameraMute))
				conferenceControl.SetCameraMute(cameraPrivacyMute);
		}

		/// <summary>
		/// Updates the current call state.
		/// </summary>
		private void UpdateIsInCall()
		{
			eCallType max = ActiveConferences.SelectMany(c => EnumUtils.GetFlags(c.CallType)).MaxOrDefault();

			switch (max)
			{
				case eCallType.Unknown:
					IsInCall = eInCall.None;
					break;
				case eCallType.Audio:
					IsInCall = eInCall.Audio;
					break;
				case eCallType.Video:
					IsInCall = eInCall.Video;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		#region Conference Manager Callbacks

		/// <summary>
		/// Subscribe to the conference manager events.
		/// </summary>
		/// <param name="conferenceManager"></param>
		private void Subscribe(IConferenceManager conferenceManager)
		{
			conferenceManager.OnEnforceAutoAnswerChanged += ConferenceManagerOnEnforceAutoAnswerChanged;
			conferenceManager.OnEnforceDoNotDisturbChanged += ConferenceManagerOnEnforceDoNotDisturbChanged;
			conferenceManager.OnPrivacyMuteStatusChange += ConferenceManagerOnPrivacyMuteStatusChange;
			conferenceManager.OnCameraPrivacyMuteStatusChange += ConferenceManagerOnCameraPrivacyMuteStatusChange;
		}

		/// <summary>
		/// Called when the conference manager privacy mute status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceManagerOnPrivacyMuteStatusChange(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProviders();
		}

		/// <summary>
		/// Called when the conference manager camera privacy mute status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceManagerOnCameraPrivacyMuteStatusChange(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProviders();
		}

		/// <summary>
		/// Called when the conference manager starts/stops enforcing do-not-disturb state.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="genericEventArgs"></param>
		private void ConferenceManagerOnEnforceDoNotDisturbChanged(object sender, GenericEventArgs<eEnforceState> genericEventArgs)
		{
			UpdateProviders();
		}

		/// <summary>
		/// Called when the conference manager starts/stops enforcing auto-answer state.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="genericEventArgs"></param>
		private void ConferenceManagerOnEnforceAutoAnswerChanged(object sender, GenericEventArgs<eEnforceState> genericEventArgs)
		{
			UpdateProviders();
		}

		#endregion

		#region Dialing Provider Callbacks

		/// <summary>
		/// Subscribe to the provider events.
		/// </summary>
		/// <param name="conferenceControl"></param>
		private void Subscribe([NotNull] IConferenceDeviceControl conferenceControl)
		{
			if (conferenceControl == null)
				throw new ArgumentNullException("conferenceControl");

			conferenceControl.OnConferenceAdded += ConferenceControlOnConferenceAdded;
			conferenceControl.OnConferenceRemoved += ConferenceControlOnConferenceRemoved;
			conferenceControl.OnAutoAnswerChanged += ConferenceControlOnAutoAnswerChanged;
			conferenceControl.OnDoNotDisturbChanged += ConferenceControlOnDoNotDisturbChanged;
			conferenceControl.OnPrivacyMuteChanged += ConferenceControlOnPrivacyMuteChanged;
			conferenceControl.OnIncomingCallAdded += ConferenceControlOnIncomingCallAdded;
			conferenceControl.OnIncomingCallRemoved += ConferenceControlOnIncomingCallRemoved;
			conferenceControl.OnCameraMuteChanged += ConferenceControlOnCameraMuteChanged;

			AddConferences(conferenceControl.GetConferences());
		}

		/// <summary>
		/// Unsubscribe from the provider events.
		/// </summary>
		/// <param name="conferenceControl"></param>
		private void Unsubscribe([NotNull] IConferenceDeviceControl conferenceControl)
		{
			if (conferenceControl == null)
				throw new ArgumentNullException("conferenceControl");

			conferenceControl.OnConferenceAdded -= ConferenceControlOnConferenceAdded;
			conferenceControl.OnConferenceRemoved -= ConferenceControlOnConferenceRemoved;
			conferenceControl.OnAutoAnswerChanged -= ConferenceControlOnAutoAnswerChanged;
			conferenceControl.OnDoNotDisturbChanged -= ConferenceControlOnDoNotDisturbChanged;
			conferenceControl.OnPrivacyMuteChanged -= ConferenceControlOnPrivacyMuteChanged;
			conferenceControl.OnIncomingCallAdded -= ConferenceControlOnIncomingCallAdded;
			conferenceControl.OnIncomingCallRemoved -= ConferenceControlOnIncomingCallRemoved;
			conferenceControl.OnCameraMuteChanged -= ConferenceControlOnCameraMuteChanged;

			RemoveConferences(conferenceControl.GetConferences());
		}

		/// <summary>
		/// Called when a conference control adds a conference.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ConferenceControlOnConferenceAdded(object sender, ConferenceEventArgs args)
		{
			AddConference(args.Data);
		}

		/// <summary>
		/// Called when a conference control removes a conference.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ConferenceControlOnConferenceRemoved(object sender, ConferenceEventArgs args)
		{
			RemoveConference(args.Data);
		}

		/// <summary>
		/// Called when a conference control adds an incoming call.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ConferenceControlOnIncomingCallAdded(object sender, GenericEventArgs<IIncomingCall> eventArgs)
		{
			IConferenceDeviceControl control = sender as IConferenceDeviceControl;
			IIncomingCall incomingCall = eventArgs.Data;

			OnIncomingCallAdded.Raise(this, new ConferenceControlIncomingCallEventArgs(control, incomingCall));
		}

		/// <summary>
		/// Called when a conference control removes an incoming call.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ConferenceControlOnIncomingCallRemoved(object sender, GenericEventArgs<IIncomingCall> eventArgs)
		{
			IConferenceDeviceControl control = sender as IConferenceDeviceControl;
			IIncomingCall incomingCall = eventArgs.Data;

			OnIncomingCallRemoved.Raise(this, new ConferenceControlIncomingCallEventArgs(control, incomingCall));
		}

		/// <summary>
		/// Called when a provider privacy mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceControlOnPrivacyMuteChanged(object sender, BoolEventArgs boolEventArgs)
		{
			IConferenceDeviceControl control = sender as IConferenceDeviceControl;
			if (control == null)
				throw new InvalidOperationException("Unexpected sender");

			IConferencePoint point = null;
			m_PointsSection.Execute(() => m_Points.TryGetKey(control, out point));

			// The conference point drives the room privacy mute state
            // If Get flag is set, or if feedback is high and GetMutedOnly flag is set
			if (m_ConferenceManager.IsActive &&
			    point != null &&
			    (point.PrivacyMuteMask.HasFlag(ePrivacyMuteFeedback.Get) ||
                (boolEventArgs.Data && point.PrivacyMuteMask.HasFlag(ePrivacyMuteFeedback.GetMutedOnly))))
				m_ConferenceManager.PrivacyMuted = boolEventArgs.Data;

			UpdateProvider(control);
		}

		/// <summary>
		/// Called when a provider do-not-disturb state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceControlOnDoNotDisturbChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProvider((IConferenceDeviceControl)sender);
		}

		/// <summary>
		/// Called when a provider auto-answer state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceControlOnAutoAnswerChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProvider((IConferenceDeviceControl)sender);
		}

		/// <summary>
		/// Called when a provider camera mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceControlOnCameraMuteChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProvider((IConferenceDeviceControl)sender);
		}

		#endregion

		#region Participant Callbacks

		/// <summary>
		/// Subscribe to the participant events.
		/// </summary>
		/// <param name="participant"></param>
		private void Subscribe(IParticipant participant)
		{
			participant.OnStatusChanged += ParticipantOnStatusChanged;
			participant.OnParticipantTypeChanged += ParticipantOnParticipantTypeChanged;
			participant.OnHandRaisedChanged += ParticipantOnHandRaisedChanged;
			participant.OnSupportedParticipantFeaturesChanged += ParticipantOnSupportedParticipantFeaturesChanged;
		}

		/// <summary>
		/// Unsubscribe from the participant events.
		/// </summary>
		/// <param name="participant"></param>
		private void Unsubscribe(IParticipant participant)
		{
			participant.OnStatusChanged -= ParticipantOnStatusChanged;
			participant.OnParticipantTypeChanged -= ParticipantOnParticipantTypeChanged;
			participant.OnHandRaisedChanged -= ParticipantOnHandRaisedChanged;
			participant.OnSupportedParticipantFeaturesChanged -= ParticipantOnSupportedParticipantFeaturesChanged;
		}

		/// <summary>
		/// Called when a participant status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ParticipantOnStatusChanged(object sender, ParticipantStatusEventArgs args)
		{
			UpdateIsInCall();

			OnActiveParticipantStatusChanged.Raise(this, new ParticipantStatusEventArgs(args.Data));
		}

		/// <summary>
		/// Called when a participant status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ParticipantOnParticipantTypeChanged(object sender, EventArgs eventArgs)
		{
			UpdateIsInCall();
		}

		private void ParticipantOnHandRaisedChanged(object sender, BoolEventArgs e)
		{
			OnParticipantHandRaiseStateChanged.Raise(this);
		}

		private void ParticipantOnSupportedParticipantFeaturesChanged(object sender, ConferenceParticipantSupportedFeaturesChangedApiEventArgs args)
		{
			var participant = sender as IParticipant;
			if (participant == null)
				return;

			OnParticipantSupportedFeaturesChanged.Raise(this, participant);
		}

		#endregion

		#region Conference Callbacks

		/// <summary>
		/// Subscribe to the conference events.
		/// </summary>
		/// <param name="conference"></param>
		private void Subscribe([NotNull] IConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			conference.OnStatusChanged += ConferenceOnStatusChanged;
			conference.OnParticipantAdded += ConferenceOnParticipantAdded;
			conference.OnParticipantRemoved += ConferenceOnParticipantRemoved;
			conference.OnCallTypeChanged += ConferenceOnCallTypeChanged;
			conference.OnConferenceRecordingStatusChanged += ConferenceOnConferenceRecordingStatusChanged;

			foreach (IParticipant participant in conference.GetParticipants())
				Subscribe(participant);
		}

		/// <summary>
		/// Unsubscribe from the conference events.
		/// </summary>
		/// <param name="conference"></param>
		private void Unsubscribe([NotNull] IConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			conference.OnStatusChanged -= ConferenceOnStatusChanged;
			conference.OnParticipantAdded -= ConferenceOnParticipantAdded;
			conference.OnParticipantRemoved -= ConferenceOnParticipantRemoved;
			conference.OnCallTypeChanged -= ConferenceOnCallTypeChanged;
			conference.OnConferenceRecordingStatusChanged -= ConferenceOnConferenceRecordingStatusChanged;

			foreach (IParticipant participant in conference.GetParticipants())
				Unsubscribe(participant);
		}

		/// <summary>
		/// Called when the active conference status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ConferenceOnStatusChanged(object sender, ConferenceStatusEventArgs args)
		{
			UpdateIsInCall();
			OnActiveConferenceStatusChanged.Raise(this, args.Data);
		}

		/// <summary>
		/// Called when a provider adds a participant to the conference.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ConferenceOnParticipantAdded(object sender, ParticipantEventArgs args)
		{
			IConference conference = sender as IConference;

			Subscribe(args.Data);
			UpdateIsInCall();
			OnConferenceParticipantAddedOrRemoved.Raise(this, new ConferenceParticipantAddedOrRemovedEventArgs(conference, true, args.Data));
		}

		private void ConferenceOnParticipantRemoved(object sender, ParticipantEventArgs args)
		{
			IConference conference = sender as IConference;

			Unsubscribe(args.Data);
			UpdateIsInCall();
			OnConferenceParticipantAddedOrRemoved.Raise(this, new ConferenceParticipantAddedOrRemovedEventArgs(conference, false, args.Data));
		}

		private void ConferenceOnCallTypeChanged(object sender, GenericEventArgs<eCallType> e)
		{
			UpdateIsInCall();
		}

		private void ConferenceOnConferenceRecordingStatusChanged(object sender, ConferenceRecordingStatusEventArgs e)
		{
			IConference conference = sender as IConference;
			if (conference == null)
				return;

			OnConferenceRecordStateChanged.Raise(this, conference);
		}

		#endregion
	}
}
