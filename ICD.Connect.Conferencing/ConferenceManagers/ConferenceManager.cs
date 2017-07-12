﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;
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
		public event EventHandler<ConferenceEventArgs> OnActiveConferenceSourcesChanged;
		public event EventHandler<ConferenceStatusEventArgs> OnActiveConferenceStatusChanged;

		public event EventHandler<ConferenceSourceEventArgs> OnRecentSourceAdded;
		public event EventHandler<ConferenceSourceStatusEventArgs> OnActiveSourceStatusChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteStatusChange;
		public event EventHandler<BoolEventArgs> OnInCallChanged;
		public event EventHandler<BoolEventArgs> OnInVideoCallChanged;

		private readonly ScrollQueue<IConference> m_RecentConferences;
		private readonly ScrollQueue<IConferenceSource> m_RecentSources;
		private readonly Dictionary<eConferenceSourceType, IDialingDeviceControl> m_SourceTypeToProvider;

		private readonly SafeCriticalSection m_RecentConferencesSection;
		private readonly SafeCriticalSection m_RecentSourcesSection;
		private readonly SafeCriticalSection m_SourceTypeToProviderSection;

		private readonly DialingPlan m_DialingPlan;

		private IConference m_ActiveConference;
		private IDialingDeviceControl m_DefaultDialingControl;

		private bool m_PrivacyMuted;

		#region Properties

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

				Unsubscribe(m_ActiveConference);
				m_ActiveConference = value;
				Subscribe(m_ActiveConference);

				UpdateIsInCall();

				OnActiveConferenceChanged.Raise(this, new ConferenceEventArgs(m_ActiveConference));
			}
		}

		/// <summary>
		/// Gets the AutoAnswer state.
		/// </summary>
		public bool AutoAnswer { get; private set; }

		/// <summary>
		/// Gets the current microphone mute state.
		/// </summary>
		public bool PrivacyMuted
		{
			get { return m_PrivacyMuted; }
			private set
			{
				if (value == m_PrivacyMuted)
					return;

				m_PrivacyMuted = value;

				OnPrivacyMuteStatusChange.Raise(this, new BoolEventArgs(m_PrivacyMuted));
			}
		}

		/// <summary>
		/// Gets the DoNotDisturb state.
		/// </summary>
		public bool DoNotDisturb { get; private set; }

		/// <summary>
		/// Returns true if actively in a video call.
		/// </summary>
		public bool IsInVideoCall { get; private set; }

		/// <summary>
		/// Returns true if actively in a call.
		/// </summary>
		public bool IsInCall { get; private set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ConferenceManager()
		{
			m_RecentConferences = new ScrollQueue<IConference>(RECENT_LENGTH);
			m_RecentSources = new ScrollQueue<IConferenceSource>(RECENT_LENGTH);
			m_SourceTypeToProvider = new Dictionary<eConferenceSourceType, IDialingDeviceControl>();

			m_RecentConferencesSection = new SafeCriticalSection();
			m_RecentSourcesSection = new SafeCriticalSection();
			m_SourceTypeToProviderSection = new SafeCriticalSection();

			m_DialingPlan = new DialingPlan();
		}

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
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
			dialingControl.Dial(number, mode);
		}

		/// <summary>
		/// Enables DoNotDisturb.
		/// </summary>
		/// <param name="state"></param>
		public void EnableDoNotDisturb(bool state)
		{
			foreach (IDialingDeviceControl provider in GetDialingProviders())
				provider.SetDoNotDisturb(state);
		}

		/// <summary>
		/// Enables AutoAnswer.
		/// </summary>
		/// <param name="state"></param>
		public void EnableAutoAnswer(bool state)
		{
			foreach (IDialingDeviceControl provider in GetDialingProviders())
				provider.SetAutoAnswer(state);
		}

		/// <summary>
		/// Enables privacy mute.
		/// </summary>
		/// <param name="state"></param>
		public void EnablePrivacyMute(bool state)
		{
			foreach (IDialingDeviceControl provider in GetDialingProviders())
				provider.SetPrivacyMute(state);
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
			m_SourceTypeToProviderSection.Enter();

			try
			{
				if (!m_SourceTypeToProvider.ContainsKey(sourceType))
					return false;

				IDialingDeviceControl dialingControl = m_SourceTypeToProvider[sourceType];
				m_SourceTypeToProvider.Remove(sourceType);

				Unsubscribe(dialingControl);
			}
			finally
			{
				m_SourceTypeToProviderSection.Leave();
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
		/// <param name="except">Avoid updating the given provider to avoid feedback loop.</param>
		private void UpdateProviders(IDialingDeviceControl except)
		{
			foreach (IDialingDeviceControl provider in GetDialingProviders().Where(p => p != except))
				UpdateProvider(provider);
		}

		/// <summary>
		/// Updates the dialing provider to match the state of the conference manager.
		/// </summary>
		/// <param name="dialingControl"></param>
		private void UpdateProvider(IDialingDeviceControl dialingControl)
		{
			dialingControl.SetAutoAnswer(AutoAnswer);
			dialingControl.SetDoNotDisturb(DoNotDisturb);
			dialingControl.SetPrivacyMute(PrivacyMuted);
		}

		/// <summary>
		/// Checks for change in the IsInCall and IsInVideoCall states and raises events.
		/// </summary>
		private void UpdateIsInCall()
		{
			IConferenceSource[] online = m_ActiveConference == null
				                             ? new IConferenceSource[0]
				                             : m_ActiveConference.GetOnlineSources();

			bool isInCall = online.Length > 0;
			bool isInVideoCall = online.Any(s => s.SourceType == eConferenceSourceType.Video);

			bool raiseInCall = isInCall != IsInCall;
			bool raiseInVideoCall = isInVideoCall != IsInVideoCall;

			// Update both at the same time for the sake of callbacks.
			IsInCall = isInCall;
			IsInVideoCall = isInVideoCall;

			if (raiseInCall)
				OnInCallChanged.Raise(this, new BoolEventArgs(IsInCall));
			if (raiseInVideoCall)
				OnInVideoCallChanged.Raise(this, new BoolEventArgs(IsInVideoCall));
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
			PrivacyMuted = GetDialingProviders().Any(p => p.PrivacyMuted);
			UpdateProviders(sender as IDialingDeviceControl);
		}

		/// <summary>
		/// Called when a provider do-not-disturb state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ProviderOnDoNotDisturbChanged(object sender, BoolEventArgs boolEventArgs)
		{
			DoNotDisturb = GetDialingProviders().Any(p => p.DoNotDisturb);
			UpdateProviders(sender as IDialingDeviceControl);
		}

		/// <summary>
		/// Called when a provider auto-answer state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ProviderOnAutoAnswerChanged(object sender, BoolEventArgs boolEventArgs)
		{
			AutoAnswer = GetDialingProviders().Any(p => p.AutoAnswer);
			UpdateProviders(sender as IDialingDeviceControl);
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

			m_ActiveConference.AddSource(source);

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

		private void ConferenceOnSourcesChanged(object sender, EventArgs eventArgs)
		{
			OnActiveConferenceSourcesChanged.Raise(this, new ConferenceEventArgs(sender as IConference));
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

			switch (args.Data)
			{
				case eConferenceStatus.Connected:
				case eConferenceStatus.Connecting:
				case eConferenceStatus.OnHold:
				case eConferenceStatus.Undefined:
					return;

				case eConferenceStatus.Disconnected:
					ActiveConference = null;
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
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
