using System;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.ConferenceManagers.History
{
	public interface IHistoricalParticipant
	{
		string Name { get; }
		string Number { get; }
		DateTime? StartTime { get; }
		DateTime? EndTime { get; }
		eCallDirection Direction { get; }
		eCallAnswerState AnswerState { get; }
		eCallType CallType { get; }
	}
}
