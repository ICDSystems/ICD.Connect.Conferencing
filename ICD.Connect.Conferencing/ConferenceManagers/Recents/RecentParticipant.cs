using System;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.ConferenceManagers.Recents
{
	public sealed class RecentParticipant : AbstractRecentCall
	{
		private readonly IParticipant m_Participant;

		public override string Name { get { return m_Participant.Name; } }

		public override string Number
		{
			get
			{
				ITraditionalParticipant traditional = m_Participant as ITraditionalParticipant;
				return traditional != null ? traditional.Number : null;
			}
		}

		public override DateTime Time
		{
			get
			{
				ITraditionalParticipant traditional = m_Participant as ITraditionalParticipant;

				if (m_Participant.End.HasValue)
					return m_Participant.End.Value;

				if (traditional != null)
					return traditional.GetStartOrDialTime();

				return m_Participant.Start ?? default(DateTime);
			}
		}

		public override eCallDirection Direction
		{
			get
			{
				ITraditionalParticipant traditional = m_Participant as ITraditionalParticipant;
				return traditional != null ? traditional.Direction : eCallDirection.Undefined;
			}
		}

		public override eCallAnswerState AnswerState
		{
			get
			{
				ITraditionalParticipant traditional = m_Participant as ITraditionalParticipant;
				return traditional != null ? traditional.AnswerState : eCallAnswerState.Unknown;
			}
		}

		public override eCallType CallType { get; }

		public IParticipant Participant { get { return m_Participant; } }

		public RecentParticipant(IParticipant participant)
		{
			m_Participant = participant;
		}
	}
}
