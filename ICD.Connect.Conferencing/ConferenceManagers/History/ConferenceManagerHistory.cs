using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.ConferenceManagers.History
{
	public sealed class ConferenceManagerHistory : IDisposable
	{
		private const int MAX_HISTORY = 50;

		public event EventHandler OnHistoryItemsChanged;

		public event EventHandler<HistoricalConferenceEventArgs> OnConferenceAdded;

		public event EventHandler<HistoricalConferenceEventArgs> OnConferenceRemoved;
		
		/// <summary>
		/// Conference manager this is running for
		/// </summary>
		private readonly ConferenceManager m_Manager;

		/// <summary>
		/// Syncronization for m_ConferencesHistory and m_ConferenceParticipantsHistory
		/// </summary>
		private readonly SafeCriticalSection m_HistorySection;

		/// <summary>
		/// Keeps history of the last conferences
		/// </summary>
		private readonly ScrollQueue<IHistoricalConference> m_ConferencesHistory;

		private readonly BiDictionary<IConference, HistoricalConference> m_ActiveConferencesMap;

		private readonly BiDictionary<IIncomingCall, HistoricalIncomingConference> m_IncomingCallsMap;


		public ConferenceManagerHistory(ConferenceManager manager)
		{
			m_Manager = manager;

			m_ConferencesHistory = new ScrollQueue<IHistoricalConference>(MAX_HISTORY);
			m_ActiveConferencesMap = new BiDictionary<IConference, HistoricalConference>();
			m_IncomingCallsMap = new BiDictionary<IIncomingCall, HistoricalIncomingConference>();
			m_HistorySection = new SafeCriticalSection();

			Subscribe(m_ConferencesHistory);

			m_Manager = manager;
			Subscribe(m_Manager);

		}

		public IEnumerable<IHistoricalConference> GetHistory()
		{
			return m_HistorySection.Execute(() => m_ConferencesHistory.ToList(m_ConferencesHistory.Count));
		}

		#region ConferenceHistory Callbacks

		private void Subscribe(ScrollQueue<IHistoricalConference> conferencesHistory)
		{
			if (conferencesHistory == null)
				return;

			conferencesHistory.OnItemTrimmed += ConferencesHistoryOnOnItemTrimmed;
		}

		/// <summary>
		/// In the unlikely event a conference gets trimmed while it's still active, detach/remove it.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ConferencesHistoryOnOnItemTrimmed(object sender, GenericEventArgs<IHistoricalConference> args)
		{
			// Be sure HistoricalConfereces are detached before removing them
			HistoricalConference historicalConference = args.Data as HistoricalConference;
			if (historicalConference != null)
			{
				DetachConference(historicalConference);
			}

			// Incoming calls are not added to the history before detached, so this is not necesary for them

			OnHistoryItemsChanged.Raise(this);
			OnConferenceRemoved.Raise(this, new HistoricalConferenceEventArgs(args.Data));

		}

		#endregion

		#region ConferenceManager Callbacks

		private void Subscribe(ConferenceManager manager)
		{
			if (manager == null)
				return;

			manager.Dialers.OnConferenceAdded += DialersOnOnConferenceAdded;
			manager.Dialers.OnConferenceRemoved += DialersOnOnConferenceRemoved;
			manager.Dialers.OnIncomingCallAdded += DialersOnOnIncomingCallAdded;
			manager.Dialers.OnIncomingCallRemoved += DialersOnOnIncomingCallRemoved;
		}

		private void Unsubscribe(ConferenceManager manager)
		{
			if (manager == null)
				return;

			manager.Dialers.OnConferenceAdded -= DialersOnOnConferenceAdded;
			manager.Dialers.OnConferenceRemoved -= DialersOnOnConferenceRemoved;
			manager.Dialers.OnIncomingCallAdded -= DialersOnOnIncomingCallAdded;
			manager.Dialers.OnIncomingCallRemoved -= DialersOnOnIncomingCallRemoved;
		}

		private void DialersOnOnConferenceAdded(object sender, ConferenceEventArgs args)
		{
			AddConference(args.Data);
		}

		private void DialersOnOnConferenceRemoved(object sender, ConferenceEventArgs args)
		{
			DetachConference(args.Data);
		}

		private void DialersOnOnIncomingCallAdded(object sender, ConferenceControlIncomingCallEventArgs args)
		{
			AddIncomingCall(args.IncomingCall);
		}

		private void DialersOnOnIncomingCallRemoved(object sender, ConferenceControlIncomingCallEventArgs args)
		{
			ProcessIncomingCallRemoved(args.IncomingCall);
		}

		#endregion

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			Unsubscribe(m_Manager);
			//todo: Dispose History
		}

		#region IConference Methods

		private void AddConference([NotNull] IConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			HistoricalConference historicalConference;

			m_HistorySection.Enter();

			try
			{
				if (m_ActiveConferencesMap.ContainsKey(conference))
					return;
				historicalConference = new HistoricalConference(conference);
				m_ActiveConferencesMap.Add(conference, historicalConference);
				m_ConferencesHistory.Enqueue(historicalConference);
			}
			finally
			{
				m_HistorySection.Leave();
			}

			OnHistoryItemsChanged.Raise(this);
			OnConferenceAdded.Raise(this, new HistoricalConferenceEventArgs(historicalConference));
		}

		private void DetachConference([NotNull]IConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			m_HistorySection.Enter();
			try
			{
				HistoricalConference historicalConference;
				if (m_ActiveConferencesMap.TryGetValue(conference, out historicalConference))
					DetachConference(historicalConference);

			}
			finally
			{
				m_HistorySection.Leave();
			}
		}

		private void DetachConference([NotNull] HistoricalConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			m_HistorySection.Enter();
			try
			{
				conference.Detach();

				m_ActiveConferencesMap.RemoveValue(conference);

			}
			finally
			{
				m_HistorySection.Leave();
			}
		}

		#endregion

		#region IIncomingCall Methods

		private void AddIncomingCall([NotNull] IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				throw new ArgumentNullException("incomingCall");

			m_HistorySection.Enter();

			try
			{
				if (m_IncomingCallsMap.ContainsKey(incomingCall))
					return;

				m_IncomingCallsMap.Add(incomingCall, new HistoricalIncomingConference(incomingCall));

			}
			finally
			{
				m_HistorySection.Leave();
			}
		}

		private void ProcessIncomingCallRemoved([NotNull]IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				throw new ArgumentNullException("incomingCall");

			HistoricalIncomingConference historicalIncomingConference;

			m_HistorySection.Enter();

			try
			{
				// If this is no longer an active incoming conference, we don't need to worry about it
				if (!m_IncomingCallsMap.TryGetValue(incomingCall, out historicalIncomingConference))
					return;

				m_IncomingCallsMap.RemoveKey(incomingCall);
				historicalIncomingConference.Detach();

				//If call was answered, don't add it to the history (we assume participant will be added)
				if (historicalIncomingConference.AnswerState == eCallAnswerState.Answered ||
				    historicalIncomingConference.AnswerState == eCallAnswerState.AutoAnswered)
					return;

				m_ConferencesHistory.Enqueue(historicalIncomingConference);
			}
			finally
			{
				m_HistorySection.Leave();
			}

			//Raise events for this added
			OnHistoryItemsChanged.Raise(this);
			OnConferenceAdded.Raise(this, new HistoricalConferenceEventArgs(historicalIncomingConference));
		}

		#endregion
	}
}
