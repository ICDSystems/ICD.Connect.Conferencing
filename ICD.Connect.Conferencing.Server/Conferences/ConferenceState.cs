using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Server.Conferences
{
	public sealed class ConferenceState
	{
		/// <summary>
		/// Current conference status.
		/// </summary>
		[PublicAPI, JsonProperty]
		public eConferenceStatus Status { get; private set; }

		/// <summary>
		/// Name of the conference.
		/// </summary>
		[PublicAPI, JsonProperty]
		public string Name { get; private set; }

		/// <summary>
		/// Number of the conference.
		/// </summary>
		[PublicAPI, JsonProperty]
		public string Number { get; private set; }

		/// <summary>
		/// The time the conference started.
		/// </summary>
		[PublicAPI, JsonProperty]
		public DateTime? StartTime { get; private set; }

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		[PublicAPI, JsonProperty]
		public DateTime? EndTime { get; private set; }

		/// <summary>
		/// Gets the type of call.
		/// </summary>
		[PublicAPI, JsonProperty]
		public eCallType CallType { get; private set; }

		/// <summary>
		/// Gets the status of the conference recording.
		/// </summary>
		[PublicAPI, JsonProperty]
		public eConferenceRecordingStatus RecordingStatus { get; private set; }

		/// <summary>
		/// Gets the supported conference features.
		/// </summary>
		[PublicAPI, JsonProperty]
		public eConferenceFeatures SupportedConferenceFeatures { get; private set; }

		[PublicAPI, JsonProperty]
		public string Language { get; set; }

		[PublicAPI, JsonProperty]
		public ParticipantState ParticipantStates { get; private set; }

		public static ConferenceState FromConference([NotNull] IConference conference, [CanBeNull] string language)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			if (!(conference is ThinConference))
				throw new ArgumentOutOfRangeException("conference");

			var participant = conference.GetParticipants().First();

			ConferenceState conferenceState = new ConferenceState
			{
				Status = conference.Status,
				Name = string.Format("({0}) {1}", language, conference.Name),
				Number = conference.Number,
				StartTime = conference.StartTime,
				EndTime = conference.EndTime,
				CallType = conference.CallType,
				RecordingStatus = conference.RecordingStatus,
				SupportedConferenceFeatures = conference.SupportedConferenceFeatures,
				Language = language,
				ParticipantStates = ParticipantState.FromParticipant(participant)
			};

			return conferenceState;
		}
	}
}