using System;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.EventArguments;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.ConferenceSources
{
	public sealed class ConferenceSourceState
	{
		[PublicAPI, JsonProperty]
		public string Name { get; private set; }
		[PublicAPI, JsonProperty]
		public string Number { get; private set; }
		[PublicAPI, JsonProperty]
		public eConferenceSourceType SourceType { get; private set; }
		[PublicAPI, JsonProperty]
		public eConferenceSourceStatus Status { get; private set; }
		[PublicAPI, JsonProperty]
		public eConferenceSourceDirection Direction { get; private set; }
		[PublicAPI, JsonProperty]
		public eConferenceSourceAnswerState AnswerState { get; private set; }
		[PublicAPI, JsonProperty]
		public DateTime? Start { get; private set; }
		[PublicAPI, JsonProperty]
		public DateTime? End { get; private set; }
		[PublicAPI, JsonProperty]
		public DateTime DialTime { get; private set; }
		[PublicAPI, JsonProperty]
		public DateTime StartOrDialTime { get; private set; }
		[PublicAPI, JsonProperty]
		public string Language { get; set; }

		[JsonConstructor]
		public ConferenceSourceState()
		{
		}

		public static ConferenceSourceState FromSource(IConferenceSource source)
		{
			if (source == null)
				throw new ArgumentNullException("source");

			return new ConferenceSourceState
			{
				Name = source.Name,
				Number = source.Number,
				SourceType = source.SourceType,
				Status = source.Status,
				Direction = source.Direction,
				AnswerState = source.AnswerState,
				Start = source.Start,
				End = source.End,
				DialTime = source.DialTime,
				StartOrDialTime = source.StartOrDialTime
			};
		}
	}
}
