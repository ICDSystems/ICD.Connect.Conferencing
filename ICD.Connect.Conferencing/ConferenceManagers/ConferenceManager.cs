﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialingPlans;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Favorites;

namespace ICD.Connect.Conferencing.ConferenceManagers
{
	/// <summary>
	/// The ConferenceManager contains an IDialingPlan and a collection of IDialingProviders
	/// to place calls and manage an active conference.
	/// </summary>
	public sealed class ConferenceManager : IConferenceManager, IDisposable
	{
		private const int RECENT_LENGTH = 100;

		public event EventHandler<ConferenceEventArgs> OnRecentConferenceAdded;
		public event EventHandler<ConferenceEventArgs> OnActiveConferenceChanged;
		public event EventHandler<ConferenceEventArgs> OnActiveConferenceEnded;
		public event EventHandler<ConferenceStatusEventArgs> OnActiveConferenceStatusChanged;
		public event EventHandler OnConferenceSourceAddedOrRemoved;

		public event EventHandler<ConferenceSourceEventArgs> OnRecentSourceAdded;
		public event EventHandler<ConferenceSourceStatusEventArgs> OnActiveSourceStatusChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteStatusChange;
		public event EventHandler<InCallEventArgs> OnInCallChanged;

		public event EventHandler<ConferenceProviderEventArgs> OnProviderAdded;
		public event EventHandler<ConferenceProviderEventArgs> OnProviderRemoved;

		private readonly ScrollQueue<IConference> m_RecentConferences;
		private readonly ScrollQueue<IConferenceSource> m_RecentSources;
		private readonly Dictionary<eConferenceSourceType, IDialingDeviceControl> m_SourceTypeToProvider;
		private readonly IcdHashSet<IDialingDeviceControl> m_FeedbackProviders; 

		private readonly SafeCriticalSection m_RecentConferencesSection;
		private readonly SafeCriticalSection m_SourcesSection;
		private readonly SafeCriticalSection m_RecentSourcesSection;
		private readonly SafeCriticalSection m_SourceTypeToProviderSection;
		private readonly SafeCriticalSection m_FeedbackProviderSection;

		private readonly DialingPlan m_DialingPlan;

		private IConference m_ActiveConference;
		private IDialingDeviceControl m_DefaultDialingControl;

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
		/// Gets the active conference.
		/// </summary>
		public IConference ActiveConference
		{
			get { return m_ActiveConference; }
			private set
			{
				if (value == m_ActiveConference)
					return;

				var oldConference = m_ActiveConference;

				Unsubscribe(m_ActiveConference);
				m_ActiveConference = value;
				Subscribe(m_ActiveConference);

				UpdateIsInCall();

				OnActiveConferenceChanged.Raise(this, new ConferenceEventArgs(m_ActiveConference));

				if (m_ActiveConference == null)
					OnActiveConferenceEnded.Raise(this, new ConferenceEventArgs(oldConference));
			}
		}

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
		/// Gets the number of registered dialling providers.
		/// </summary>
		public int DialingProvidersCount
		{
			get
			{
				m_SourceTypeToProviderSection.Enter();

				try
				{
					return m_SourceTypeToProvider.Values
					                             .Distinct()
					                             .Count();
				}
				finally
				{
					m_SourceTypeToProviderSection.Leave();
				}
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ConferenceManager()
		{
			m_RecentConferences = new ScrollQueue<IConference>(RECENT_LENGTH);
			m_RecentSources = new ScrollQueue<IConferenceSource>(RECENT_LENGTH);
			m_SourceTypeToProvider = new Dictionary<eConferenceSourceType, IDialingDeviceControl>();
			m_FeedbackProviders = new IcdHashSet<IDialingDeviceControl>();

			m_RecentConferencesSection = new SafeCriticalSection();
			m_SourcesSection = new SafeCriticalSection();
			m_RecentSourcesSection = new SafeCriticalSection();
			m_SourceTypeToProviderSection = new SafeCriticalSection();
			m_FeedbackProviderSection = new SafeCriticalSection();

			m_DialingPlan = new DialingPlan();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnRecentConferenceAdded = null;
			OnActiveConferenceChanged = null;
			OnActiveConferenceStatusChanged = null;
			OnRecentSourceAdded = null;
			OnActiveSourceStatusChanged = null;
			OnPrivacyMuteStatusChange = null;
			OnInCallChanged = null;

			ClearDialingProviders();

			Unsubscribe(ActiveConference);
		}

		/// <summary>
		/// Dials the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="mode"></param>
		public void Dial(string number, eConferenceSourceType mode)
		{
			if (mode == eConferenceSourceType.Unknown)
				mode = DialingPlan.DefaultSourceType;

			IDialingDeviceControl dialingControl = GetDialingProvider(mode);
			if (dialingControl == null)
			{
				Logger.AddEntry(eSeverity.Error,
				                "{0} failed to dial number {1} with mode {2} - No matching dialing control could be found",
				                GetType().Name,
				                StringUtils.ToRepresentation(number),
				                mode);
				return;
			}

			mode = EnumUtils.GetFlagsIntersection(dialingControl.Supports, mode);
			if (mode == eConferenceSourceType.Unknown)
				mode = dialingControl.Supports;

			dialingControl.Dial(number, mode);
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
		/// Gets the recent conferences in order of time.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConference> GetRecentConferences()
		{
			return m_RecentConferencesSection.Execute(() => m_RecentConferences.ToArray());
		}

		/// <summary>
		/// Gets the recent sources in order of time.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IConferenceSource> GetRecentSources()
		{
			return m_RecentSourcesSection.Execute(() => m_RecentSources.ToArray());
		}

		/// <summary>
		/// Gets the dialing provider for the given source type.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns></returns>
		public IDialingDeviceControl GetDialingProvider(eConferenceSourceType sourceType)
		{
			return
				m_SourceTypeToProviderSection.Execute(() => m_SourceTypeToProvider.GetDefault(sourceType, m_DefaultDialingControl));
		}

		/// <summary>
		/// Gets the registered dialing providers.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IDialingDeviceControl> GetDialingProviders()
		{
			return m_SourceTypeToProviderSection.Execute(() => m_SourceTypeToProvider.Values.ToArray());
		}

		/// <summary>
		/// Gets the registered feedback dialing providers.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IDialingDeviceControl> GetFeedbackDialingProviders()
		{
			return m_FeedbackProviderSection.Execute(() => m_FeedbackProviders.ToArray());
		}

		/// <summary>
		/// Registers the dialing provider.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <param name="dialingControl"></param>
		/// <returns></returns>
		public bool RegisterDialingProvider(eConferenceSourceType sourceType, IDialingDeviceControl dialingControl)
		{
			if (m_DefaultDialingControl == null)
				m_DefaultDialingControl = dialingControl;

			m_SourceTypeToProviderSection.Enter();

			try
			{
				if (m_SourceTypeToProvider.ContainsKey(sourceType))
					return false;

				m_SourceTypeToProvider[sourceType] = dialingControl;
				UpdateProvider(dialingControl);

				Subscribe(dialingControl);
			}
			finally
			{
				m_SourceTypeToProviderSection.Leave();
			}

			OnProviderAdded.Raise(this, new ConferenceProviderEventArgs(sourceType, dialingControl));

			AddSources(dialingControl.GetSources());

			return true;
		}

		/// <summary>
		/// Deregisters the dialing provider.
		/// </summary>
		/// <param name="sourceType"></param>
		/// <returns></returns>
		public bool DeregisterDialingProvider(eConferenceSourceType sourceType)
		{
			IDialingDeviceControl dialingControl;
			m_SourceTypeToProviderSection.Enter();

			try
			{
				if (!m_SourceTypeToProvider.ContainsKey(sourceType))
					return false;

				dialingControl = m_SourceTypeToProvider[sourceType];
				m_SourceTypeToProvider.Remove(sourceType);

				Unsubscribe(dialingControl);
			}
			finally
			{
				m_SourceTypeToProviderSection.Leave();
			}

			OnProviderRemoved.Raise(this, new ConferenceProviderEventArgs(sourceType, dialingControl));

			return true;
		}

		/// <summary>
		/// Registers the dialing component, for feedback only.
		/// </summary>
		/// <param name="dialingControl"></param>
		/// <returns></returns>
		public bool RegisterFeedbackDialingProvider(IDialingDeviceControl dialingControl)
		{
			m_FeedbackProviderSection.Enter();
			try
			{
				if (m_FeedbackProviders.Contains(dialingControl))
					return false;

				m_FeedbackProviders.Add(dialingControl);
				UpdateFeedbackProvider(dialingControl);

				Subscribe(dialingControl);
			}
			finally
			{
				m_FeedbackProviderSection.Leave();
			}

			AddSources(dialingControl.GetSources());

			return true;
		}

		/// <summary>
		/// Deregisters the dialing componet from the feedback only list.
		/// </summary>
		/// <param name="dialingControl"></param>
		/// <returns></returns>
		public bool DeregisterFeedbackDialingProvider(IDialingDeviceControl dialingControl)
		{
			m_FeedbackProviderSection.Enter();
			try
			{
				if (!m_FeedbackProviders.Contains(dialingControl))
					return false;

				m_FeedbackProviders.Remove(dialingControl);

				Unsubscribe(dialingControl);
			}
			finally
			{
				m_FeedbackProviderSection.Leave();
			}

			return true;
		}

		/// <summary>
		/// Deregisters all of the dialing components.
		/// </summary>
		public void ClearDialingProviders()
		{
			eConferenceSourceType[] sourceTypes =
				m_SourceTypeToProviderSection.Execute(() => m_SourceTypeToProvider.Keys.ToArray());

			foreach (eConferenceSourceType sourceType in sourceTypes)
				DeregisterDialingProvider(sourceType);

			foreach (IDialingDeviceControl provider in m_FeedbackProviders.ToArray(m_FeedbackProviders.Count))
				DeregisterFeedbackDialingProvider(provider);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Adds the conference to the recent conferences collection.
		/// </summary>
		/// <param name="conference"></param>
		private void AddConference(IConference conference)
		{
			m_RecentConferencesSection.Execute(() => m_RecentConferences.Enqueue(conference));
			OnRecentConferenceAdded.Raise(this, new ConferenceEventArgs(conference));
		}

		/// <summary>
		/// Updates the dialing providers to match the state of the conference manager.
		/// </summary>
		private void UpdateProviders()
		{
			foreach (IDialingDeviceControl provider in GetDialingProviders())
				UpdateProvider(provider);

			foreach (IDialingDeviceControl provider in GetFeedbackDialingProviders())
				UpdateFeedbackProvider(provider);
		}

		/// <summary>
		/// Updates the feedback dialing provider to match the state of the conference manager.
		/// </summary>
		/// <param name="dialingControl"></param>
		private void UpdateFeedbackProvider(IDialingDeviceControl dialingControl)
		{
			bool privacyMute = PrivacyMuted;

			if (dialingControl.PrivacyMuted != privacyMute)
				dialingControl.SetPrivacyMute(privacyMute);
		}

		/// <summary>
		/// Updates the dialing provider to match the state of the conference manager.
		/// </summary>
		/// <param name="dialingControl"></param>
		private void UpdateProvider(IDialingDeviceControl dialingControl)
		{
			bool autoAnswer = AutoAnswer;
			bool doNotDisturb = DoNotDisturb;
			bool privacyMute = PrivacyMuted;

			if (dialingControl.AutoAnswer != autoAnswer)
				dialingControl.SetAutoAnswer(autoAnswer);

			if (dialingControl.DoNotDisturb != doNotDisturb)
				dialingControl.SetDoNotDisturb(doNotDisturb);

			if (dialingControl.PrivacyMuted != privacyMute)
				dialingControl.SetPrivacyMute(privacyMute);
		}

		/// <summary>
		/// Updates the current call state.
		/// </summary>
		private void UpdateIsInCall()
		{
			IEnumerable<IConferenceSource> online = m_ActiveConference == null
				                                        ? Enumerable.Empty<IConferenceSource>()
				                                        : m_ActiveConference.GetOnlineSources();

			eInCall inCall = eInCall.None;

			foreach (IConferenceSource source in online)
			{
				switch (source.SourceType)
				{
					case eConferenceSourceType.Unknown:
					case eConferenceSourceType.Audio:
						inCall = eInCall.Audio;
						break;

					case eConferenceSourceType.Video:
						inCall = eInCall.Video;
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}

				if (inCall == eInCall.Video)
					break;
			}

			IsInCall = inCall;
		}

		#endregion

		#region Dialing Provider Callbacks

		/// <summary>
		/// Subscribe to the provider events.
		/// </summary>
		/// <param name="dialingControl"></param>
		private void Subscribe(IDialingDeviceControl dialingControl)
		{
			if (dialingControl == null)
				return;

			dialingControl.OnSourceAdded += ProviderOnSourceAdded;
			dialingControl.OnSourceRemoved += ProviderOnSourceRemoved;
			dialingControl.OnAutoAnswerChanged += ProviderOnAutoAnswerChanged;
			dialingControl.OnDoNotDisturbChanged += ProviderOnDoNotDisturbChanged;
			dialingControl.OnPrivacyMuteChanged += ProviderOnPrivacyMuteChanged;
		}

		/// <summary>
		/// Unsubscribe from the provider events.
		/// </summary>
		/// <param name="dialingControl"></param>
		private void Unsubscribe(IDialingDeviceControl dialingControl)
		{
			if (dialingControl == null)
				return;

			dialingControl.OnSourceAdded -= ProviderOnSourceAdded;
			dialingControl.OnSourceRemoved -= ProviderOnSourceRemoved;
			dialingControl.OnAutoAnswerChanged -= ProviderOnAutoAnswerChanged;
			dialingControl.OnDoNotDisturbChanged -= ProviderOnDoNotDisturbChanged;
			dialingControl.OnPrivacyMuteChanged -= ProviderOnPrivacyMuteChanged;
		}

		/// <summary>
		/// Called when a provider privacy mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ProviderOnPrivacyMuteChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProvider(sender as IDialingDeviceControl);
		}

		/// <summary>
		/// Called when a provider do-not-disturb state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ProviderOnDoNotDisturbChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProvider(sender as IDialingDeviceControl);
		}

		/// <summary>
		/// Called when a provider auto-answer state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ProviderOnAutoAnswerChanged(object sender, BoolEventArgs boolEventArgs)
		{
			UpdateProvider(sender as IDialingDeviceControl);
		}

		/// <summary>
		/// Called when a provider adds a source to the conference.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ProviderOnSourceAdded(object sender, ConferenceSourceEventArgs args)
		{
			AddSource(args.Data);
		}

		private void ProviderOnSourceRemoved(object sender, ConferenceSourceEventArgs args)
		{
			UpdateIsInCall();
		}

		/// <summary>
		/// Adds the sequence of sources to the active conference, creating a new
		///	conference if one does not yet exist.
		/// </summary>
		/// <param name="sources"></param>
		private void AddSources(IEnumerable<IConferenceSource> sources)
		{
			foreach (IConferenceSource source in sources)
				AddSource(source);
		}

		/// <summary>
		/// Adds the source to the active conference. Creates a new conference if one
		/// does not yet exist.
		/// </summary>
		/// <param name="source"></param>
		private void AddSource(IConferenceSource source)
		{
			if (!this.GetIsActiveConferenceOnline())
			{
				ActiveConference = new Conference();
				AddConference(ActiveConference);
			}

			if (m_ActiveConference.ContainsSource(source))
				return;

			m_SourcesSection.Enter();
			try
			{
				m_ActiveConference.AddSource(source);
			}
			finally
			{
				m_SourcesSection.Leave();
			}

			m_RecentSourcesSection.Enter();

			try
			{
				if (!m_RecentSources.Contains(source))
					m_RecentSources.Enqueue(source);
			}
			finally
			{
				m_RecentSourcesSection.Leave();
			}

			Subscribe(source);

			UpdateIsInCall();

			OnRecentSourceAdded.Raise(this, new ConferenceSourceEventArgs(source));
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
			conference.OnSourcesChanged += ConferenceOnSourcesChanged;
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
			conference.OnSourcesChanged -= ConferenceOnSourcesChanged;

			// Unsubscribe from the sources.
			foreach (IConferenceSource source in conference.GetSources())
				Unsubscribe(source);
		}

		/// <summary>
		/// Called when the active conference status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ConferenceOnStatusChanged(object sender, ConferenceStatusEventArgs args)
		{
			OnActiveConferenceStatusChanged.Raise(this, new ConferenceStatusEventArgs(args.Data));

			if (args.Data == eConferenceStatus.Disconnected)
			{
				ActiveConference = null;
			}
		}

		private void ConferenceOnSourcesChanged(object sender, EventArgs eventArgs)
		{
			OnConferenceSourceAddedOrRemoved.Raise(this);
		}

		#endregion

		#region Source Callbacks

		/// <summary>
		/// Subscribe to the source events.
		/// </summary>
		/// <param name="source"></param>
		private void Subscribe(IConferenceSource source)
		{
			source.OnStatusChanged += SourceOnStatusChanged;
			source.OnSourceTypeChanged += SourceOnSourceTypeChanged;
		}

		/// <summary>
		/// Unsubscribe from the source events.
		/// </summary>
		/// <param name="source"></param>
		private void Unsubscribe(IConferenceSource source)
		{
			source.OnStatusChanged -= SourceOnStatusChanged;
			source.OnSourceTypeChanged -= SourceOnSourceTypeChanged;
		}

		/// <summary>
		/// Called when a source status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void SourceOnStatusChanged(object sender, ConferenceSourceStatusEventArgs args)
		{
			UpdateIsInCall();

			OnActiveSourceStatusChanged.Raise(this, new ConferenceSourceStatusEventArgs(args.Data));
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
