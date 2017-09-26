using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.EventArguments;
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
		public event EventHandler<ConferenceStatusEventArgs> OnActiveConferenceStatusChanged;

		public event EventHandler<ConferenceSourceEventArgs> OnRecentSourceAdded;
		public event EventHandler<ConferenceSourceStatusEventArgs> OnActiveSourceStatusChanged;
		public event EventHandler<BoolEventArgs> OnPrivacyMuteStatusChange;
		public event EventHandler<InCallEventArgs> OnInCallChanged;

		private readonly ScrollQueue<IConference> m_RecentConferences;
		private readonly ScrollQueue<IConferenceSource> m_RecentSources;
		private readonly Dictionary<eConferenceSourceType, IDialingDeviceControl> m_SourceTypeToProvider;

		private readonly SafeCriticalSection m_RecentConferencesSection;
		private readonly SafeCriticalSection m_RecentSourcesSection;
		private readonly SafeCriticalSection m_SourceTypeToProviderSection;

		private readonly DialingPlan m_DialingPlan;

		private IConference m_ActiveConference;
		private IDialingDeviceControl m_DefaultDialingControl;

		private eInCall m_IsInCall;

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
		private void UpdateProviders()
		{
			foreach (IDialingDeviceControl provider in GetDialingProviders())
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
				ActiveConference = null;
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
