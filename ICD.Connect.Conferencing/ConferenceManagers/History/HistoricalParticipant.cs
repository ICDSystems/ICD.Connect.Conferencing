using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

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

			var traditionalParticipant = participant as ITraditionalParticipant;

			Name = participant.Name;
			Number = traditionalParticipant == null ? null : traditionalParticipant.Number;
			StartTime = participant.StartTime;
			EndTime = participant.EndTime;
			Direction = traditionalParticipant == null ? eCallDirection.Undefined : traditionalParticipant.Direction;
			AnswerState = traditionalParticipant == null
				              ? eCallAnswerState.Unknown
				              : traditionalParticipant.AnswerState;
			CallType = participant.CallType;
		}

		#region Participant Callbacks

		public void Subscribe(IParticipant participant)
		{
			if (participant == null)
				return;

			participant.OnNameChanged += ParticipantOnOnNameChanged;
			participant.OnParticipantTypeChanged += ParticipantOnOnParticipantTypeChanged;
			participant.OnStartTimeChanged += ParticipantOnOnStartTimeChanged;
			participant.OnEndTimeChanged += ParticipantOnOnEndTimeChanged;

			var traditionalParticipant = participant as ITraditionalParticipant;
			if (traditionalParticipant != null)
			{
				traditionalParticipant.OnNumberChanged += TraditionalParticipantOnOnNumberChanged;
				traditionalParticipant.OnAnswerStateChanged += TraditionalParticipantOnOnAnswerStateChanged;
			}
		}

		public void Unsubscribe(IParticipant participant)
		{
			if (participant == null)
				return;

			participant.OnNameChanged -= ParticipantOnOnNameChanged;
			participant.OnParticipantTypeChanged -= ParticipantOnOnParticipantTypeChanged;
			participant.OnStartTimeChanged -= ParticipantOnOnStartTimeChanged;
			participant.OnEndTimeChanged -= ParticipantOnOnEndTimeChanged;

			var traditionalParticipant = participant as ITraditionalParticipant;
			if (traditionalParticipant != null)
			{
				traditionalParticipant.OnNumberChanged -= TraditionalParticipantOnOnNumberChanged;
				traditionalParticipant.OnAnswerStateChanged -= TraditionalParticipantOnOnAnswerStateChanged;
			}
		}

		private void ParticipantOnOnNameChanged(object sender, StringEventArgs args)
		{
			Name = args.Data;
		}

		private void ParticipantOnOnParticipantTypeChanged(object sender, CallTypeEventArgs args)
		{
			CallType = args.Data;
		}

		private void ParticipantOnOnStartTimeChanged(object sender, DateTimeNullableEventArgs args)
		{
			StartTime = args.Data;
		}

		private void ParticipantOnOnEndTimeChanged(object sender, DateTimeNullableEventArgs args)
		{
			EndTime = args.Data;
		}

		private void TraditionalParticipantOnOnNumberChanged(object sender, StringEventArgs args)
		{
			Number = args.Data;
		}

		private void TraditionalParticipantOnOnAnswerStateChanged(object sender, CallAnswerStateEventArgs args)
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
