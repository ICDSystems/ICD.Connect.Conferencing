using System;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.ConferenceManagers.Recents
{
	public interface IRecentCall
	{
		string Name { get; }
		string Number { get; }
		DateTime Time { get; }
		eCallDirection Direction { get; }
		eCallAnswerState AnswerState { get; }
		eCallType CallType { get; }
	}
}
