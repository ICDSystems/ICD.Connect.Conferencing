#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Server.Devices
{
	public sealed class ParticipantState
	{
		[PublicAPI, JsonProperty]
		public string Name { get; private set; }
		[PublicAPI, JsonProperty]
		public string Number { get; private set; }
		[PublicAPI, JsonProperty]
		public eCallType SourceType { get; private set; }
		[PublicAPI, JsonProperty]
		public eParticipantStatus Status { get; private set; }
		[PublicAPI, JsonProperty]
		public eCallDirection Direction { get; private set; }
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
		public ParticipantState()
		{
		}

		public static ParticipantState FromParticipant(ITraditionalParticipant participant, string language)
		{
			if (participant == null)
				throw new ArgumentNullException("source");

			return new ParticipantState
			{
				Name = participant.Name,
				Number = participant.Number,
				SourceType = participant.CallType,
				Status = participant.Status,
				Direction = participant.Direction,
				Start = participant.StartTime,
				End = participant.EndTime,
				DialTime = participant.DialTime,
				StartOrDialTime = participant.GetStartOrDialTime(),
				Language = language
			};
		}
	}
}
