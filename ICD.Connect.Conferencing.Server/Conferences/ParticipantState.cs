using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Participants.Enums;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Server.Conferences
{
	public sealed class ParticipantState
	{
		/// <summary>
		/// These features aren't supported on participants in the conferencing server/client
		/// because they would require calling methods on the participant or returning more complex objects
		/// </summary>
		public const eParticipantFeatures UNSUPPORTED_FEATURES =
													eParticipantFeatures.GetCamera |
													eParticipantFeatures.Admit |
													eParticipantFeatures.Kick |
													eParticipantFeatures.RaiseLowerHand |
													eParticipantFeatures.SetMute;

		[PublicAPI, JsonProperty]
		public string Name { get; private set; }
		[PublicAPI, JsonProperty]
		public string Number { get; private set; }
		[PublicAPI, JsonProperty]
		public eCallType CallType { get; private set; }
		[PublicAPI, JsonProperty]
		public eParticipantStatus Status { get; private set; }
		[PublicAPI, JsonProperty]
		public eCallDirection Direction { get; private set; }
		[PublicAPI, JsonProperty]
		public DateTime? StartTime { get; private set; }
		[PublicAPI, JsonProperty]
		public DateTime? EndTime { get; private set; }
		[PublicAPI, JsonProperty]
		public DateTime DialTime { get; private set; }
		[PublicAPI, JsonProperty]
		public DateTime StartOrDialTime { get; private set; }

		/// <summary>
		/// Whether or not the participant is muted.
		/// </summary>
		[PublicAPI, JsonProperty]
		public bool IsMuted { get; private set; }

		/// <summary>
		/// Whether or not the participant is the room itself.
		/// </summary>
		[PublicAPI, JsonProperty]
		public bool IsSelf { get; private set; }

		/// <summary>
		/// Whether or not the participant is the meeting host.
		/// </summary>
		[PublicAPI, JsonProperty]
		public bool IsHost { get; private set; }

		[PublicAPI, JsonProperty]
		public eParticipantFeatures SupportedParticipantFeatures { get; private set; }
		

		[JsonConstructor]
		public ParticipantState()
		{
		}

		public static ParticipantState FromParticipant(IParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			return new ParticipantState
			{
				Name = participant.Name,
				Number = participant.Number,
				CallType = participant.CallType,
				Status = participant.Status,
				Direction = participant.Direction,
				StartTime = participant.StartTime,
				EndTime = participant.EndTime,
				DialTime = participant.DialTime,
				StartOrDialTime = participant.GetStartOrDialTime(),
				IsMuted = participant.IsMuted,
				IsSelf = participant.IsSelf,
				IsHost = participant.IsHost,
				SupportedParticipantFeatures = participant.SupportedParticipantFeatures.ExcludeFlags(UNSUPPORTED_FEATURES)
			};
		}
	}

	public static class ParticipantStateExtensions
	{
		public static ThinParticipant ToThinParticipant([NotNull] this ParticipantState extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");
			
			ThinParticipant participant = new ThinParticipant();
			participant.UpdateFromParticipantState(extends);
			return participant;
		}

		public static void UpdateFromParticipantState([NotNull] this ThinParticipant extends,
		                                                          [NotNull] ParticipantState state)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");
			if (state == null)
				throw new ArgumentNullException("state");

			
			extends.Name = state.Name;
			extends.Number = state.Number;
			extends.CallType = state.CallType;
			extends.Status = state.Status;
			extends.Direction = state.Direction;
			extends.StartTime = state.StartTime;
			extends.EndTime = state.EndTime;
			extends.DialTime = state.DialTime;
			extends.IsMuted = state.IsMuted;
			extends.IsSelf = state.IsSelf;
			extends.IsHost = state.IsHost;
			extends.SupportedParticipantFeatures = state.SupportedParticipantFeatures.ExcludeFlags(ParticipantState.UNSUPPORTED_FEATURES);
		}
	}
}
