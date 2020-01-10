using System;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.ConferenceManagers.Recents
{
	public abstract class AbstractRecentCall : IRecentCall
	{
		public abstract string Name { get; }
		public abstract string Number { get; }
		public abstract DateTime Time { get; }
		public abstract eCallDirection Direction { get; }
		public abstract eCallAnswerState AnswerState { get; }
		public abstract eCallType CallType { get; }
	}
}
