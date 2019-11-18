using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.DialingPlans;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Favorites;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.ConferenceManagers
{
	/// <summary>
	/// The ConferenceManager contains an IDialingPlan and a collection of IConferenceDeviceControls
	/// to place calls and manage the active conferences.
	/// </summary>
	public sealed class ConferenceManager : IConferenceManager, IDisposable
	{
		private const int RECENT_LENGTH = 100;

		public event EventHandler<BoolEventArgs> OnIsAuthoritativeChanged;

		public event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		public event EventHandler<ConferenceStatusEventArgs> OnActiveConferenceStatusChanged;
		public event EventHandler OnConferenceParticipantAddedOrRemoved;
		public event EventHandler<ConferenceProviderEventArgs> OnProviderAdded;
		public event EventHandler<ConferenceProviderEventArgs> OnProviderRemoved;
		public event EventHandler<ConferenceControlIncomingCallEventArgs> OnIncomingCallAdded;
		public event EventHandler<ConferenceControlIncomingCallEventArgs> OnIncomingCallRemoved;

		public event EventHandler<ParticipantEventArgs> OnRecentParticipantAdded;
		public event EventHandler<ParticipantStatusEventArgs> OnActiveParticipantStatusChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteStatusChange;
		public event EventHandler<InCallEventArgs> OnInCallChanged;

		private readonly IcdHashSet<IConference> m_Conferences;
		private readonly ScrollQueue<IParticipant> m_RecentParticipants;
		private readonly Dictionary<IConferenceDeviceControl, eCallType> m_DialingProviders; 
		private readonly IcdHashSet<IConferenceDeviceControl> m_FeedbackProviders;

		private readonly SafeCriticalSection m_ConferencesSection;
		private readonly SafeCriticalSection m_RecentParticipantsSection;
		private readonly SafeCriticalSection m_DialingProviderSection;
		private readonly SafeCriticalSection m_FeedbackProviderSection;

		private readonly DialingPlan m_DialingPlan;

		private eInCall m_IsInCall;
		private bool m_IsAuthoritative;

		#region Properties

		/// <summary>
		/// Gets the logger.
		/// </summary>
		public ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

		/// <summary>
		/// When true the conference manager will force registered dialers to match
		/// the state of the Privacy Mute, Do Not Disturb and Auto Answer properties.
		/// </summary>
		public bool IsAuthoritative
		{
			get { return m_IsAuthoritative; }
			set
			{
				if (value == m_IsAuthoritative)
					return;

				m_IsAuthoritative = value;

				OnIsAuthoritativeChanged.Raise(this, new BoolEventArgs(m_IsAuthoritative));
			}
		}

		/// <summary>
		/// Gets the dialing plan.
		/// </summary>
		public DialingPlan DialingPlan { get { return m_DialingPlan; } }

		/// <summary>
		/// Gets the favorites.
		/// </summary>
		public IFavorites Favorites { get; set; }

		/// <summary>
		/// Gets the active conferences.
		/// </summary>
		public IEnumerable<IConference> ActiveConferences { get { return m_Conferences.Where(c => c.IsActive()); } }

		/// <summary>
		/// Gets the online conferences.
		/// </summary>
		public IEnumerable<IConference> OnlineConferences { get { return m_Conferences.Where(c => c.IsOnline()); } }

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		public bool AutoAnswer { get; private set; }

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		public bool PrivacyMuted { get; private set; }

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		public bool DoNotDisturb { get; private set; }

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

				Logger.AddEntry(eSeverity.Informational, "{0} call state changed to {1}", GetType().Name, m_IsInCall);

				OnInCallChanged.Raise(this, new InCallEventArgs(m_IsInCall));
			}
		}

		/// <summary>
		/// Gets the number of registered dialing providers.
		/// </summary>
		public int DialingProvidersCount { get { return m_DialingProviderSection.Execute(() => m_DialingProviders.Count); } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ConferenceManager()
		{
			m_IsAuthoritative = true;

			m_Conferences = new IcdHashSet<IConference>();
			m_RecentParticipants = new ScrollQueue<IParticipant>(RECENT_LENGTH);

			m_DialingProviders = new Dictionary<IConferenceDeviceControl, eCallType>();
			m_FeedbackProviders = new IcdHashSet<IConferenceDeviceControl>();

			m_ConferencesSection = new SafeCriticalSection();
			m_RecentParticipantsSection = new SafeCriticalSection();
			m_DialingProviderSection = new SafeCriticalSection();
			m_FeedbackProviderSection = new SafeCriticalSection();

			m_DialingPlan = new DialingPlan();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnIsAuthoritativeChanged = null;
			OnConferenceAdded = null;
			OnConferenceRemoved = null;
			OnActiveConferenceStatusChanged = null;
			OnConferenceParticipantAddedOrRemoved = null;
			OnProviderAdded = null;
			OnProviderRemoved = null;
			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;
			OnRecentParticipantAdded = null;
			OnActiveParticipantStatusChanged = null;
			OnPrivacyMuteStatusChange = null;
			OnInCallChanged = null;

			ClearDialingProviders();

			RemoveConferences(m_Conferences.ToList());
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="dialContext"></param>
		public void Dial(IDialContext dialContext)
		{
			IConferenceDeviceControl conferenceControl = GetDialingProviders().GetBestDialer(dialContext);
			if (conferenceControl == null)
			{
				Logger.AddEntry(eSeverity.Error,
				                "{0} failed to dial {1} - No matching conference control could be found",
				                GetType().Name,
				                dialContext);
				return;
			}

			conferenceControl.Dial(dialContext);
		}

		/// <summary>
		/// Enables DoNotDisturb.
		/// </summary>
		/// <param name="state"></param>
		public void EnableDoNotDisturb(bool state)
		{
			if (state == DoNotDisturb)
				return;

			DoNotDisturb = state;

			UpdateProviders();
		}

		/// <summary>
		/// Enables AutoAnswer.
		/// </summary>
		/// <param name="state"></param>
		public void EnableAutoAnswer(bool state)
		{
			if (state == AutoAnswer)
				return;

			AutoAnswer = state;

			UpdateProviders();
		}

		/// <summary>
		/// Enables privacy mute.
		/// </summary>
		/// <param name="state"></param>
		public void EnablePrivacyMute(bool state)
		{
			if (state == PrivacyMuted)
				return;

			PrivacyMuted = state;

			UpdateProviders();

			OnPrivacyMuteStatusChange.Raise(this, new BoolEventArgs(PrivacyMuted));
		}

		/// <summary>
		/// Gets the recent participants in order of time.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IParticipant> GetRecentParticipants()
		{
			return m_RecentParticipantsSection.Execute(() => m_RecentParticipants.ToArray());
		}

		/// <summary>
		/// Gets the registered conference providers.
		/// </summary>
		/// <returns></returns>
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
		/// Gets the registered feedback conference providers.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConferenceDeviceControl> GetFeedbackDialingProviders()
		{
			return m_FeedbackProviderSection.Execute(() => m_FeedbackProviders.ToArray());
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

		/// <summary>
		/// Deregisters all of the conference controls.
		/// </summary>
		public void ClearDialingProviders()
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
					AddRecentParticipant(participant);
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
		/// Adds the participant to the recent participants collection.
		/// </summary>
		/// <param name="participant"></param>
		private void AddRecentParticipant([NotNull] IParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			m_RecentParticipantsSection.Execute(() => m_RecentParticipants.Enqueue(participant));

			// TODO - Why?
			Subscribe(participant);

			OnRecentParticipantAdded.Raise(this, new ParticipantEventArgs(participant));
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

			if (!m_IsAuthoritative)
				return;

			bool privacyMute = PrivacyMuted;
			if (conferenceControl.PrivacyMuted != privacyMute)
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

			if (!m_IsAuthoritative)
				return;

			bool autoAnswer = AutoAnswer;
			if (conferenceControl.AutoAnswer != autoAnswer)
				conferenceControl.SetAutoAnswer(autoAnswer);

			bool doNotDisturb = DoNotDisturb;
			if (conferenceControl.DoNotDisturb != doNotDisturb)
				conferenceControl.SetDoNotDisturb(doNotDisturb);

			bool privacyMute = PrivacyMuted;
			if (conferenceControl.PrivacyMuted != privacyMute)
				conferenceControl.SetPrivacyMute(privacyMute);
		}

		/// <summary>
		/// Updates the current call state.
		/// </summary>
		private void UpdateIsInCall()
		{
			if (!OnlineConferences.Any())
				IsInCall = eInCall.None;
			else
				IsInCall = (eInCall)OnlineConferences.Max(c => (int)c.CallType);
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
			OnIncomingCallAdded.Raise(this, new ConferenceControlIncomingCallEventArgs(control, eventArgs.Data));
		}

		/// <summary>
		/// Called when a conference control removes an incoming call.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void ConferenceControlOnIncomingCallRemoved(object sender, GenericEventArgs<IIncomingCall> eventArgs)
		{
			IConferenceDeviceControl control = sender as IConferenceDeviceControl;
			OnIncomingCallRemoved.Raise(this, new ConferenceControlIncomingCallEventArgs(control, eventArgs.Data));
		}

		/// <summary>
		/// Called when a provider privacy mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceControlOnPrivacyMuteChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProvider(sender as IConferenceDeviceControl);
		}

		/// <summary>
		/// Called when a provider do-not-disturb state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceControlOnDoNotDisturbChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProvider(sender as IConferenceDeviceControl);
		}

		/// <summary>
		/// Called when a provider auto-answer state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConferenceControlOnAutoAnswerChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProvider(sender as IConferenceDeviceControl);
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
			AddRecentParticipant(args.Data);

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
	}
}
