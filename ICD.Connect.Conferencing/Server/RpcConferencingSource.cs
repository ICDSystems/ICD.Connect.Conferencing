using System;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Server
{
	public class RpcConferenceSource
	{
		[PublicAPI]
		public string Name { get; }
		[PublicAPI]
		public string Number { get; }
		[PublicAPI]
		public eConferenceSourceType SourceType { get; }
		[PublicAPI]
		public eConferenceSourceStatus Status { get; }
		[PublicAPI]
		public eConferenceSourceDirection Direction { get; }
		[PublicAPI]
		public eConferenceSourceAnswerState AnswerState { get; }
		[PublicAPI]
		public DateTime? Start { get; }
		[PublicAPI]
		public DateTime? End { get; }
		[PublicAPI]
		public DateTime DialTime { get; }
		[PublicAPI]
		public DateTime StartOrDialTime { get; }

		public RpcConferenceSource(IConferenceSource source)
		{
			Name = source.Name;
			Number = source.Number;
			SourceType = source.SourceType;
			Status = source.Status;
			Direction = source.Direction;
			AnswerState = source.AnswerState;
			Start = source.Start;
			End = source.End;
			DialTime = source.DialTime;
			StartOrDialTime = source.StartOrDialTime;
		}
	}
}
