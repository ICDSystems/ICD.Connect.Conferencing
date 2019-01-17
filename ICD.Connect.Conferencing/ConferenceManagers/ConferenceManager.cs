using System;
using System.Collections.Generic;
using System.Linq;
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
	/// The ConferenceManager contains an IDialingPlan and a collection of IDialingProviders
	/// to place calls and manage an active conference.
	/// </summary>
	public sealed class ConferenceManager : IConferenceManager, IDisposable
	{
		private const int RECENT_LENGTH = 100;

		public event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public event EventHandler<ConferenceEventArgs> OnConferenceRemoved;
		public event EventHandler<ConferenceStatusEventArgs> OnActiveConferenceStatusChanged;
		public event EventHandler OnConferenceSourceAddedOrRemoved;
		public event EventHandler<ConferenceProviderEventArgs> OnProviderAdded;
		public event EventHandler<ConferenceProviderEventArgs> OnProviderRemoved;

		public event EventHandler<ParticipantEventArgs> OnRecentSourceAdded;
		public event EventHandler<ParticipantStatusEventArgs> OnActiveSourceStatusChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteStatusChange;
		public event EventHandler<InCallEventArgs> OnInCallChanged;

		private readonly IcdHashSet<IConference> m_Conferences;
		private readonly ScrollQueue<IParticipant> m_RecentSources;
		private readonly Dictionary<IConferenceDeviceControl, eCallType> m_DialingProviders; 
		private readonly IcdHashSet<IConferenceDeviceControl> m_FeedbackProviders;

		private readonly SafeCriticalSection m_ConferencesSection;
		private readonly SafeCriticalSection m_RecentSourcesSection;
		private readonly SafeCriticalSection m_DialingProviderSection;
		private readonly SafeCriticalSection m_FeedbackProviderSection;

		private readonly DialingPlan m_DialingPlan;

		private eInCall m_IsInCall;

		#region Properties

		/// <summary>
		/// Gets the logger.
		/// </summary>
		public ILoggerService Logger { get { return ServiceProvider.TryGetService<ILoggerService>(); } }

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
			m_Conferences = new IcdHashSet<IConference>();
			m_RecentSources = new ScrollQueue<IParticipant>(RECENT_LENGTH);

			m_DialingProviders = new Dictionary<IConferenceDeviceControl, eCallType>();
			m_FeedbackProviders = new IcdHashSet<IConferenceDeviceControl>();

			m_ConferencesSection = new SafeCriticalSection();
			m_RecentSourcesSection = new SafeCriticalSection();
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
			OnConferenceAdded = null;
			OnActiveConferenceStatusChanged = null;
			OnRecentSourceAdded = null;
			OnActiveSourceStatusChanged = null;
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
		/// Gets the recent sources in order of time.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IParticipant> GetRecentSources()
		{
			return m_RecentSourcesSection.Execute(() => m_RecentSources.ToArray());
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
		/// <param name="sourceType"></param>
		/// <returns></returns>
		public IEnumerable<IConferenceDeviceControl> GetDialingProviders(eCallType sourceType)
		{
			m_DialingProviderSection.Enter();

			try
			{
				return m_DialingProviders.Where(kvp => kvp.Value.HasFlags(sourceType))
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
		public bool RegisterDialingProvider(IConferenceDeviceControl conferenceControl, eCallType callType)
		{
			if (conferenceControl == null)
				throw new ArgumentNullException("conferenceControl");

			if (!conferenceControl.Supports.HasFlags(callType))
				throw new ArgumentException("Conference control does not support given call type.");

			m_DialingProviderSection.Enter();

			try
			{
				if (m_DialingProviders.ContainsKey(conferenceControl))
					return false;

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
		public bool RegisterFeedbackDialingProvider(IConferenceDeviceControl conferenceControl)
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
				if (!m_FeedbackProviders.Contains(conferenceControl))
					return false;

				Unsubscribe(conferenceControl);

				m_FeedbackProviders.Remove(conferenceControl);
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

		private void AddConferences(IEnumerable<IConference> conferences)
		{
			foreach (IConference conference in conferences)
				AddConference(conference);
		}

		/// <summary>
		/// Adds the conference to the recent conferences collection.
		/// </summary>
		/// <param name="conference"></param>
		private void AddConference(IConference conference)
		{
			if (conference == null)
				return;

			m_ConferencesSection.Enter();

			try
			{
				if (!m_Conferences.Add(conference))
					return;

				Subscribe(conference);

				OnConferenceAdded.Raise(this, new ConferenceEventArgs(conference));

				foreach (IParticipant participant in conference.GetParticipants())
					AddParticipant(participant);
			}
			finally
			{
				m_ConferencesSection.Leave();
			}

			UpdateIsInCall();
		}

		private void RemoveConferences(IEnumerable<IConference> conferences)
		{
			foreach (IConference conference in conferences)
				RemoveConference(conference);
		}

		private void RemoveConference(IConference conference)
		{
			if (conference == null)
				return;

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

		private void AddParticipant(IParticipant participant)
		{
			m_RecentSourcesSection.Execute(() => m_RecentSources.Enqueue(participant));
			OnRecentSourceAdded.Raise(this, new ParticipantEventArgs(participant));

			Subscribe(participant);
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
		private void UpdateFeedbackProvider(IConferenceDeviceControl conferenceControl)
		{
			bool privacyMute = PrivacyMuted;

			if (conferenceControl.PrivacyMuted != privacyMute)
				conferenceControl.SetPrivacyMute(privacyMute);
		}

		/// <summary>
		/// Updates the conference provider to match the state of the conference manager.
		/// </summary>
		/// <param name="conferenceControl"></param>
		private void UpdateProvider(IConferenceDeviceControl conferenceControl)
		{
			bool autoAnswer = AutoAnswer;
			bool doNotDisturb = DoNotDisturb;
			bool privacyMute = PrivacyMuted;

			if (conferenceControl.AutoAnswer != autoAnswer)
				conferenceControl.SetAutoAnswer(autoAnswer);

			if (conferenceControl.DoNotDisturb != doNotDisturb)
				conferenceControl.SetDoNotDisturb(doNotDisturb);

			if (conferenceControl.PrivacyMuted != privacyMute)
				conferenceControl.SetPrivacyMute(privacyMute);
		}

		/// <summary>
		/// Updates the current call state.
		/// </summary>
		private void UpdateIsInCall()
		{
			IsInCall = (eInCall)OnlineConferences.Max(c => (int)c.CallType);
		}

		#endregion

		#region Dialing Provider Callbacks

		/// <summary>
		/// Subscribe to the provider events.
		/// </summary>
		/// <param name="conferenceControl"></param>
		private void Subscribe(IConferenceDeviceControl conferenceControl)
		{
			if (conferenceControl == null)
				return;

			conferenceControl.OnConferenceAdded += ConferenceControlOnConferenceAdded;
			conferenceControl.OnConferenceRemoved += ConferenceControlOnConferenceRemoved;
			conferenceControl.OnAutoAnswerChanged += ConferenceControlOnAutoAnswerChanged;
			conferenceControl.OnDoNotDisturbChanged += ConferenceControlOnDoNotDisturbChanged;
			conferenceControl.OnPrivacyMuteChanged += ConferenceControlOnPrivacyMuteChanged;

			AddConferences(conferenceControl.GetConferences());
		}

		/// <summary>
		/// Unsubscribe from the provider events.
		/// </summary>
		/// <param name="conferenceControl"></param>
		private void Unsubscribe(IConferenceDeviceControl conferenceControl)
		{
			if (conferenceControl == null)
				return;

			conferenceControl.OnConferenceAdded -= ConferenceControlOnConferenceAdded;
			conferenceControl.OnConferenceRemoved -= ConferenceControlOnConferenceRemoved;
			conferenceControl.OnAutoAnswerChanged -= ConferenceControlOnAutoAnswerChanged;
			conferenceControl.OnDoNotDisturbChanged -= ConferenceControlOnDoNotDisturbChanged;
			conferenceControl.OnPrivacyMuteChanged -= ConferenceControlOnPrivacyMuteChanged;

			RemoveConferences(conferenceControl.GetConferences());
		}

		private void ConferenceControlOnConferenceAdded(object sender, ConferenceEventArgs args)
		{
			AddConference(args.Data);
		}

		private void ConferenceControlOnConferenceRemoved(object sender, ConferenceEventArgs args)
		{
			RemoveConference(args.Data);
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
		private void Subscribe(IConference conference)
		{
			if (conference == null)
				return;

			conference.OnStatusChanged += ConferenceOnStatusChanged;
			conference.OnParticipantAdded += ConferenceOnParticipantAdded;
			conference.OnParticipantRemoved += ConferenceOnParticipantRemoved;
			
			// Subscribe to the sources.
			foreach (IParticipant source in conference.GetParticipants())
				Subscribe(source);
		}

		/// <summary>
		/// Unsubscribe from the conference events.
		/// </summary>
		/// <param name="conference"></param>
		private void Unsubscribe(IConference conference)
		{
			if (conference == null)
				return;

			conference.OnStatusChanged -= ConferenceOnStatusChanged;
			conference.OnParticipantAdded -= ConferenceOnParticipantAdded;
			conference.OnParticipantRemoved -= ConferenceOnParticipantRemoved;

			// Unsubscribe from the sources.
			foreach (IParticipant source in conference.GetParticipants())
				Unsubscribe(source);
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
		/// Called when a provider adds a source to the conference.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ConferenceOnParticipantAdded(object sender, ParticipantEventArgs args)
		{
			Subscribe(args.Data);
			UpdateIsInCall();
			OnConferenceSourceAddedOrRemoved.Raise(this);
		}

		private void ConferenceOnParticipantRemoved(object sender, ParticipantEventArgs args)
		{
			Unsubscribe(args.Data);
			UpdateIsInCall();
			OnConferenceSourceAddedOrRemoved.Raise(this);
		}

		#endregion

		#region Source Callbacks

		/// <summary>
		/// Subscribe to the source events.
		/// </summary>
		/// <param name="source"></param>
		private void Subscribe(IParticipant source)
		{
			source.OnStatusChanged += SourceOnStatusChanged;
			source.OnSourceTypeChanged += SourceOnSourceTypeChanged;
		}

		/// <summary>
		/// Unsubscribe from the source events.
		/// </summary>
		/// <param name="source"></param>
		private void Unsubscribe(IParticipant source)
		{
			source.OnStatusChanged -= SourceOnStatusChanged;
			source.OnSourceTypeChanged -= SourceOnSourceTypeChanged;
		}

		/// <summary>
		/// Called when a source status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void SourceOnStatusChanged(object sender, ParticipantStatusEventArgs args)
		{
			UpdateIsInCall();

			OnActiveSourceStatusChanged.Raise(this, new ParticipantStatusEventArgs(args.Data));
		}

		/// <summary>
		/// Called when a source status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SourceOnSourceTypeChanged(object sender, EventArgs eventArgs)
		{
			UpdateIsInCall();
		}

		#endregion
	}
}
