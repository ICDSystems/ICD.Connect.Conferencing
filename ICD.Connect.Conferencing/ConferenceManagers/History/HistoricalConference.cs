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
using ICD.Connect.Conferencing.Participants.Enums;

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
		private string m_Name;
		private string m_Number;
		private eCallDirection m_Direction;
		private eCallAnswerState m_AnswerState;

		private Type m_CachedConferenceType;

		#endregion

		#region Properties

		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		public event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		public event EventHandler<StringEventArgs> OnNameChanged;
		public event EventHandler<StringEventArgs> OnNumberChanged;
		public event EventHandler<GenericEventArgs<eCallDirection>> OnDirectionChanged;
		public event EventHandler<GenericEventArgs<eCallAnswerState>> OnAnswerStateChanged;

		public string Name
		{
			get { return m_Name; }
			private set
			{
				if (m_Name == value)
					return;

				m_Name = value;

				OnNameChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Number of the conferenc for redial, etc
		/// </summary>
		public string Number
		{
			get { return m_Number; }
			private set
			{
				if (m_Number == value)
					return;

				m_Number = value;

				OnNumberChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Direction
		/// </summary>
		public eCallDirection Direction
		{
			get { return m_Direction; }
			private set
			{
				if (m_Direction == value)
					return;

				m_Direction = value;

				OnDirectionChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Answer State
		/// </summary>
		public eCallAnswerState AnswerState
		{
			get { return m_AnswerState; }
			private set
			{
				if (m_AnswerState == value)
					return;

				m_AnswerState = value;

				OnAnswerStateChanged.Raise(this, value);
			}
		}

		/// <summary>
		/// Call Type
		/// </summary>
		public eCallType CallType { get; private set; }

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

				OnStatusChanged.Raise(this, value);
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

		public Type GetConferenceType()
		{
			return m_CachedConferenceType;
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
			Name = conference.Name;
			Number = conference.Number;
			AnswerState = conference.AnswerState;
			Direction = conference.Direction;
			Status = conference.Status;
			CallType = conference.CallType;
			foreach(IParticipant participant in conference.GetParticipants())
				AddParticipant(participant);
		}

		#region Conference Callbacks

		private void Subscribe(IConference conference)
		{
			if (conference == null)
				return;

			m_CachedConferenceType = conference.GetType();

			conference.OnNameChanged += ConferenceOnNameChanged;
			conference.OnNumberChanged += ConferenceOnNumberChanged;
			conference.OnAnswerStateChanged += ConferenceOnAnswerStateChanged;
			conference.OnDirectionChanged += ConferenceOnDirectionChanged;
			conference.OnStatusChanged += ConferenceOnOnStatusChanged;
			conference.OnParticipantAdded += ConferenceOnOnParticipantAdded;
			conference.OnParticipantRemoved += ConferenceOnOnParticipantRemoved;
			conference.OnStartTimeChanged += ConferenceOnOnStartTimeChanged;
			conference.OnEndTimeChanged += ConferenceOnOnEndTimeChanged;
			conference.OnCallTypeChanged += ConferenceOnCallTypeChanged;
		}

		private void Unsubscribe(IConference conference)
		{
			if (conference == null)
				return;

			conference.OnNameChanged -= ConferenceOnNameChanged;
			conference.OnNumberChanged -= ConferenceOnNumberChanged;
			conference.OnAnswerStateChanged -= ConferenceOnAnswerStateChanged;
			conference.OnDirectionChanged -= ConferenceOnDirectionChanged;
			conference.OnStatusChanged -= ConferenceOnOnStatusChanged;
			conference.OnParticipantAdded -= ConferenceOnOnParticipantAdded;
			conference.OnParticipantRemoved -= ConferenceOnOnParticipantRemoved;
			conference.OnStartTimeChanged -= ConferenceOnOnStartTimeChanged;
			conference.OnEndTimeChanged -= ConferenceOnOnEndTimeChanged;
			conference.OnCallTypeChanged -= ConferenceOnCallTypeChanged;
		}

		private void ConferenceOnNameChanged(object sender, StringEventArgs args)
		{
			Name = args.Data;
		}

		private void ConferenceOnNumberChanged(object sender, StringEventArgs args)
		{
			Number = args.Data;
		}

		private void ConferenceOnAnswerStateChanged(object sender, GenericEventArgs<eCallAnswerState> args)
		{
			AnswerState = args.Data;
		}

		private void ConferenceOnDirectionChanged(object sender, GenericEventArgs<eCallDirection> args)
		{
			Direction = args.Data;
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

		private void ConferenceOnCallTypeChanged(object sender, GenericEventArgs<eCallType> args)
		{
			CallType = args.Data;
		}

		#endregion
	}
}
