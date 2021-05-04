using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.ConferenceManagers.History
{
	public sealed class HistoricalParticipant : IHistoricalParticipant
	{
		private IParticipant m_Participant;

		public string Name { get; private set; }

		public string Number { get; private set; }

		public DateTime? StartTime { get; private set; }

		public DateTime? EndTime { get; private set; }

		public eCallDirection Direction { get; private set; }
		
		public eCallAnswerState AnswerState { get; private set; }
		
		public eCallType CallType { get; private set; }
		
		public HistoricalParticipant([NotNull] IParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			m_Participant = participant;
			Subscribe(m_Participant);
			UpdateState(m_Participant);
		}

		private void UpdateState([NotNull]IParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			Name = participant.Name;
			Number = participant.Number;
			StartTime = participant.StartTime;
			EndTime = participant.EndTime;
			Direction = participant.Direction;
			AnswerState = participant.AnswerState;
			CallType = participant.CallType;
		}

		#region Participant Callbacks

		public void Subscribe(IParticipant participant)
		{
			if (participant == null)
				return;

			participant.OnNameChanged += ParticipantOnNameChanged;
			participant.OnParticipantTypeChanged += ParticipantOnParticipantTypeChanged;
			participant.OnStartTimeChanged += ParticipantOnStartTimeChanged;
			participant.OnEndTimeChanged += ParticipantOnEndTimeChanged;
			participant.OnNumberChanged += ParticipantOnNumberChanged;
			participant.OnAnswerStateChanged += ParticipantOnAnswerStateChanged;

		}

		public void Unsubscribe(IParticipant participant)
		{
			if (participant == null)
				return;

			participant.OnNameChanged -= ParticipantOnNameChanged;
			participant.OnParticipantTypeChanged -= ParticipantOnParticipantTypeChanged;
			participant.OnStartTimeChanged -= ParticipantOnStartTimeChanged;
			participant.OnEndTimeChanged -= ParticipantOnEndTimeChanged;
			participant.OnNumberChanged -= ParticipantOnNumberChanged;
			participant.OnAnswerStateChanged -= ParticipantOnAnswerStateChanged;
		}

		private void ParticipantOnNameChanged(object sender, StringEventArgs args)
		{
			Name = args.Data;
		}

		private void ParticipantOnParticipantTypeChanged(object sender, CallTypeEventArgs args)
		{
			CallType = args.Data;
		}

		private void ParticipantOnStartTimeChanged(object sender, DateTimeNullableEventArgs args)
		{
			StartTime = args.Data;
		}

		private void ParticipantOnEndTimeChanged(object sender, DateTimeNullableEventArgs args)
		{
			EndTime = args.Data;
		}

		private void ParticipantOnNumberChanged(object sender, StringEventArgs args)
		{
			Number = args.Data;
		}

		private void ParticipantOnAnswerStateChanged(object sender, CallAnswerStateEventArgs args)
		{
			AnswerState = args.Data;
		}

		internal void Detach()
		{
			Unsubscribe(m_Participant);
			m_Participant = null;
		}

		#endregion
	}
}
