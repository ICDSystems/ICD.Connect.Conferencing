using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.ConferenceManagers.History
{
	public sealed class HistoricalConference : IHistoricalConference
	{
		#region Fields

		private IConference m_Conference;

		private readonly List<HistoricalParticipant> m_Participants;

		private readonly BiDictionary<IParticipant, HistoricalParticipant> m_ParticipantMap;

		private readonly SafeCriticalSection m_ParticipantsSection;

		private eConferenceStatus m_Status;

		#endregion

		#region Properties

		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		public event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;
		public DateTime? StartTime { get; private set; }
		public DateTime? EndTime { get; private set; }

		/// <summary>
		/// Gets the status of the conference
		/// </summary>
		public eConferenceStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (m_Status == value)
					return;

				m_Status = value;

				OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(value));
			}
		}

		#endregion

		public HistoricalConference([NotNull] IConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");
			
			m_Participants = new List<HistoricalParticipant>();
			m_ParticipantMap = new BiDictionary<IParticipant, HistoricalParticipant>();
			m_ParticipantsSection = new SafeCriticalSection();


			m_Conference = conference;
			UpdateConferenceValues(m_Conference);
			Subscribe(m_Conference);
		}

		#region Public Methods

		public IEnumerable<IHistoricalParticipant> GetParticipants()
		{
			return m_ParticipantsSection.Execute(() => m_Participants.ToList(m_Participants.Count).Cast<IHistoricalParticipant>());
		}

		public void Detach()
		{
			Unsubscribe(m_Conference);
			m_Conference = null;

			m_ParticipantsSection.Enter();
			try
			{
				foreach (HistoricalParticipant participant in m_ParticipantMap.Values)
				{
					participant.Detach();
				}

				m_ParticipantMap.Clear();
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		#endregion

		#region Private Methods

		private void AddParticipant(IParticipant participant)
		{
			m_ParticipantsSection.Enter();

			try
			{
				HistoricalParticipant historicalParticipant = new HistoricalParticipant(participant);
				m_Participants.Add(historicalParticipant);
				m_ParticipantMap.Add(participant, historicalParticipant);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		private void DetachParticipant(IParticipant participant)
		{
			m_ParticipantsSection.Enter();

			try
			{
				HistoricalParticipant historicalParticipant;
				if (!m_ParticipantMap.TryGetValue(participant, out historicalParticipant))
					return;

				historicalParticipant.Detach();
				m_ParticipantMap.RemoveKey(participant);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		#endregion

		private void UpdateConferenceValues(IConference conference)
		{
			foreach(IParticipant participant in conference.GetParticipants())
				AddParticipant(participant);
		}

		#region Conference Callbacks

		private void Subscribe(IConference conference)
		{
			if (conference == null)
				return;

			conference.OnStatusChanged += ConferenceOnOnStatusChanged;
			conference.OnParticipantAdded += ConferenceOnOnParticipantAdded;
			conference.OnParticipantRemoved += ConferenceOnOnParticipantRemoved;
			conference.OnStartTimeChanged += ConferenceOnOnStartTimeChanged;
			conference.OnEndTimeChanged += ConferenceOnOnEndTimeChanged;
		}

		private void Unsubscribe(IConference conference)
		{
			if (conference == null)
				return;

			conference.OnStatusChanged -= ConferenceOnOnStatusChanged;
			conference.OnParticipantAdded -= ConferenceOnOnParticipantAdded;
			conference.OnParticipantRemoved -= ConferenceOnOnParticipantRemoved;
			conference.OnStartTimeChanged -= ConferenceOnOnStartTimeChanged;
			conference.OnEndTimeChanged -= ConferenceOnOnEndTimeChanged;
		}

		private void ConferenceOnOnStatusChanged(object sender, ConferenceStatusEventArgs args)
		{
			Status = args.Data;
		}

		private void ConferenceOnOnParticipantAdded(object sender, ParticipantEventArgs args)
		{
			AddParticipant(args.Data);
		}

		private void ConferenceOnOnParticipantRemoved(object sender, ParticipantEventArgs args)
		{
			DetachParticipant(args.Data);
		}

		private void ConferenceOnOnStartTimeChanged(object sender, DateTimeNullableEventArgs args)
		{
			StartTime = args.Data;
		}

		private void ConferenceOnOnEndTimeChanged(object sender, DateTimeNullableEventArgs args)
		{
			EndTime = args.Data;
		}

		#endregion
	}
}
