using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.ConferenceManagers.Recents;
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
		private const int RECENT_LENGTH = 100;

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
		public event EventHandler OnConferenceParticipantAddedOrRemoved;

		/// <summary>
		/// Raised when a conference control is added to the manager.
		/// </summary>
		public event EventHandler<ConferenceProviderEventArgs> OnProviderAdded;

		/// <summary>
		/// Raised when a conference control is removed from the manager.
		/// </summary>
		public event EventHandler<ConferenceProviderEventArgs> OnProviderRemoved;

		/// <summary>
		/// Called when an incoming call is added by a conference control.
		/// </summary>
		public event EventHandler<ConferenceControlIncomingCallEventArgs> OnIncomingCallAdded;

		/// <summary>
		/// Called when an incoming call is removed by a conference control.
		/// </summary>
		public event EventHandler<ConferenceControlIncomingCallEventArgs> OnIncomingCallRemoved;

		/// <summary>
		/// Raised when a recent call is added to or removed from the recent calls collection.
		/// </summary>
		public event EventHandler<RecentCallEventArgs> OnRecentCallsChanged;

		private readonly IConferenceManager m_ConferenceManager;

		private readonly IcdHashSet<IConference> m_Conferences;
		private readonly List<IRecentCall> m_RecentCalls;
		private readonly Dictionary<IConferenceDeviceControl, eCallType> m_DialingProviders;
		private readonly IcdHashSet<IConferenceDeviceControl> m_FeedbackProviders;

		private readonly SafeCriticalSection m_ConferencesSection;
		private readonly SafeCriticalSection m_RecentCallsSection;
		private readonly SafeCriticalSection m_DialingProviderSection;
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

		/// <summary>
		/// Gets the number of registered dialing providers.
		/// </summary>
		public int DialingProvidersCount { get { return m_DialingProviderSection.Execute(() => m_DialingProviders.Count); } }

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
			m_RecentCalls = new List<IRecentCall>();
			m_DialingProviders = new Dictionary<IConferenceDeviceControl, eCallType>();
			m_FeedbackProviders = new IcdHashSet<IConferenceDeviceControl>();

			m_ConferencesSection = new SafeCriticalSection();
			m_RecentCallsSection = new SafeCriticalSection();
			m_DialingProviderSection = new SafeCriticalSection();
			m_FeedbackProviderSection = new SafeCriticalSection();

			Subscribe(m_ConferenceManager);
		}

		#region Methods

		/// <summary>
		/// Deregisters all of the conference controls.
		/// </summary>
		public void Clear()
		{
			m_DialingProviderSection.Enter();

			try
			{
				foreach (IConferenceDeviceControl conferenceControl in m_DialingProviders.Keys.ToArray(m_DialingProviders.Count))
					DeregisterDialingProvider(conferenceControl);
			}
			finally
			{
				m_DialingProviderSection.Leave();
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
		/// Gets the recent calls in order of time.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<IRecentCall> GetRecentCalls()
		{
			return m_RecentCallsSection.Execute(() => m_RecentCalls.ToArray());
		}

		/// <summary>
		/// Gets the registered conference providers.
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public IEnumerable<IConferenceDeviceControl> GetDialingProviders()
		{
			return m_DialingProviderSection.Execute(() => m_DialingProviders.Keys.ToArray());
		}

		/// <summary>
		/// Gets the registered conference components.
		/// </summary>
		/// <param name="callType"></param>
		/// <returns></returns>
		public IEnumerable<IConferenceDeviceControl> GetDialingProviders(eCallType callType)
		{
			m_DialingProviderSection.Enter();

			try
			{
				return m_DialingProviders.Where(kvp => kvp.Value.HasFlags(callType))
										 .Select(kvp => kvp.Key)
										 .ToArray();
			}
			finally
			{
				m_DialingProviderSection.Leave();
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

			m_DialingProviderSection.Enter();

			try
			{
				IGrouping<eDialContextSupport, IConferenceDeviceControl> bestGroup =
					m_DialingProviders.Keys
					                  .Where(c => c.Supports.HasFlags(dialContext.CallType))
					                  .GroupBy(d => d.CanDial(dialContext))
					                  .Where(g => g.Key != eDialContextSupport.Unsupported)
					                  .OrderByDescending(g => g.Key)
					                  .FirstOrDefault();

				return bestGroup == null ? null : bestGroup.FirstOrDefault();
			}
			finally
			{
				m_DialingProviderSection.Leave();
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
			                            .Where(d => d.GetActiveConference() != null);
		}

		/// <summary>
		/// Registers the dialing provider at the given conference point.
		/// </summary>
		/// <param name="conferencePoint"></param>
		public void RegisterDialingProvider([NotNull] IConferencePoint conferencePoint)
		{
			if (conferencePoint == null)
				throw new ArgumentNullException("conferencePoint");

			if (conferencePoint.Control == null)
				throw new ArgumentException("Conference point does not have a conference control");

			RegisterDialingProvider(conferencePoint.Control, conferencePoint.Type);
		}

		/// <summary>
		/// Registers the conference provider.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <param name="callType"></param>
		/// <returns></returns>
		public bool RegisterDialingProvider([NotNull] IConferenceDeviceControl conferenceControl, eCallType callType)
		{
			if (conferenceControl == null)
				throw new ArgumentNullException("conferenceControl");

			m_DialingProviderSection.Enter();

			try
			{
				eCallType oldCallType;
				if (m_DialingProviders.TryGetValue(conferenceControl, out oldCallType) && callType == oldCallType)
					return false;

				Unsubscribe(conferenceControl);
				m_DialingProviders.Add(conferenceControl, callType);
				Subscribe(conferenceControl);

				UpdateProvider(conferenceControl);
			}
			finally
			{
				m_DialingProviderSection.Leave();
			}

			OnProviderAdded.Raise(this, new ConferenceProviderEventArgs(callType, conferenceControl));

			return true;
		}

		/// <summary>
		/// Deregisters the conference provider.
		/// </summary>
		/// <param name="conferenceControl"></param>
		/// <returns></returns>
		public bool DeregisterDialingProvider(IConferenceDeviceControl conferenceControl)
		{
			if (conferenceControl == null)
				throw new ArgumentNullException("conferenceControl");

			eCallType callType;

			m_DialingProviderSection.Enter();

			try
			{
				if (!m_DialingProviders.TryGetValue(conferenceControl, out callType))
					return false;

				Unsubscribe(conferenceControl);

				m_DialingProviders.Remove(conferenceControl);
			}
			finally
			{
				m_DialingProviderSection.Leave();
			}

			OnProviderRemoved.Raise(this, new ConferenceProviderEventArgs(callType, conferenceControl));

			return true;
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

				foreach (IParticipant participant in conference.GetParticipants())
					AddRecentCall(participant);
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
		public void UpdateProviders()
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
			if (conferenceControl.PrivacyMuted != privacyMute && conferenceControl.SupportedConferenceFeatures.HasFlag(eConferenceFeatures.PrivacyMute))
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

			if (conferenceControl.SupportedConferenceFeatures.HasFlag(eConferenceFeatures.AutoAnswer))
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

			if (conferenceControl.SupportedConferenceFeatures.HasFlag(eConferenceFeatures.DoNotDisturb))
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

			bool privacyMute = m_ConferenceManager.PrivacyMuted;
			if (conferenceControl.PrivacyMuted != privacyMute && conferenceControl.SupportedConferenceFeatures.HasFlag(eConferenceFeatures.PrivacyMute))
				conferenceControl.SetPrivacyMute(privacyMute);
		}

		/// <summary>
		/// Updates the current call state.
		/// </summary>
		private void UpdateIsInCall()
		{
			IsInCall = (eInCall)ActiveConferences.Select(c => (int)c.CallType).MaxOrDefault();
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
		}

		private void ConferenceManagerOnPrivacyMuteStatusChange(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProviders();
		}

		private void ConferenceManagerOnEnforceDoNotDisturbChanged(object sender, GenericEventArgs<eEnforceState> genericEventArgs)
		{
			UpdateProviders();
		}

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

			Subscribe(incomingCall);
			AddRecentCall(incomingCall);

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

			Unsubscribe(incomingCall);

			OnIncomingCallRemoved.Raise(this, new ConferenceControlIncomingCallEventArgs(control, incomingCall));
		}

		/// <summary>
		/// Called when a provider privacy mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceControlOnPrivacyMuteChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProvider((IConferenceDeviceControl)sender);
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
		}

		/// <summary>
		/// Unsubscribe from the participant events.
		/// </summary>
		/// <param name="participant"></param>
		private void Unsubscribe(IParticipant participant)
		{
			participant.OnStatusChanged -= ParticipantOnStatusChanged;
			participant.OnParticipantTypeChanged -= ParticipantOnParticipantTypeChanged;
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

		#endregion

		#region Incoming Call Callbacks

		private void Subscribe(IIncomingCall call)
		{
			if (call == null)
				return;

			call.OnAnswerStateChanged += CallOnAnswerStateChanged;
		}

		private void Unsubscribe(IIncomingCall call)
		{
			if (call == null)
				return;

			call.OnAnswerStateChanged -= CallOnAnswerStateChanged;
		}

		private void CallOnAnswerStateChanged(object sender, IncomingCallAnswerStateEventArgs args)
		{
			if (args.Data == eCallAnswerState.Answered || args.Data == eCallAnswerState.Autoanswered)
				RemoveRecentCall((IIncomingCall)sender);
		}

		#endregion

		#region Recent Calls

		private void AddRecentCall([NotNull] IParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			AddRecentCall(new RecentParticipant(participant));
		}

		private void AddRecentCall([NotNull] IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				throw new ArgumentNullException("incomingCall");

			AddRecentCall(new RecentIncomingCall(incomingCall));
		}

		private void AddRecentCall([NotNull] IRecentCall recentCall)
		{
			if (recentCall == null)
				throw new ArgumentNullException("recentCall");

			List<IRecentCall> removed = new List<IRecentCall>();

			m_RecentCallsSection.Enter();

			try
			{
				m_RecentCalls.Add(recentCall);

				while (m_RecentCalls.Count > RECENT_LENGTH)
				{
					removed.Add(m_RecentCalls[0]);
					m_RecentCalls.RemoveAt(0);
				}
			}
			finally
			{
				m_RecentCallsSection.Leave();
			}

			foreach (IRecentCall remove in removed)
				OnRecentCallsChanged.Raise(this, new RecentCallEventArgs(remove, false));

			OnRecentCallsChanged.Raise(this, new RecentCallEventArgs(recentCall, true));
		}

		private void RemoveRecentCall(IIncomingCall incomingCall)
		{
			List<IRecentCall> removed = new List<IRecentCall>();

			m_RecentCallsSection.Enter();

			try
			{
				for (int index = m_RecentCalls.Count - 1; index >= 0; index--)
				{
					RecentIncomingCall recentIncoming = m_RecentCalls[index] as RecentIncomingCall;
					if (recentIncoming == null || recentIncoming.IncomingCall != incomingCall)
						continue;

					m_RecentCalls.RemoveAt(index);
					removed.Insert(0, recentIncoming);
				}
			}
			finally
			{
				m_RecentCallsSection.Leave();
			}

			foreach (IRecentCall remove in removed)
				OnRecentCallsChanged.Raise(this, new RecentCallEventArgs(remove, false));
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
			OnActiveConferenceStatusChanged.Raise(this, new ConferenceStatusEventArgs(args.Data));
		}

		/// <summary>
		/// Called when a provider adds a participant to the conference.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ConferenceOnParticipantAdded(object sender, ParticipantEventArgs args)
		{
			AddRecentCall(args.Data);

			Subscribe(args.Data);
			UpdateIsInCall();
			OnConferenceParticipantAddedOrRemoved.Raise(this);
		}

		private void ConferenceOnParticipantRemoved(object sender, ParticipantEventArgs args)
		{
			Unsubscribe(args.Data);
			UpdateIsInCall();
			OnConferenceParticipantAddedOrRemoved.Raise(this);
		}

		#endregion
	}
}
