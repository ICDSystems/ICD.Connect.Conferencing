using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Conferencing.Server.Conferences
{
	public delegate void InterpretationThinConferenceActionCallback(InterpretationThinConference sender);

	public delegate void InterpretationThinConferenceDtmfCallback(InterpretationThinConference sender, string dtmf);



	public sealed class InterpretationThinConference : AbstractConference<ThinParticipant>
	{
		/// <summary>
		/// Raised when a participant is added to the conference.
		/// </summary>
		public override event EventHandler<ParticipantEventArgs> OnParticipantAdded;

		/// <summary>
		/// Raised when a participant is removed from the conference.
		/// </summary>
		public override event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		private KeyValuePair<ParticipantInfo, ThinParticipant> m_Participant;

		public new eConferenceStatus Status
		{
			get { return base.Status; }
			set { base.Status = value; }
		}

		public new eConferenceFeatures SupportedConferenceFeatures
		{
			get { return base.SupportedConferenceFeatures; }
			set { base.SupportedConferenceFeatures = value; }
		}

		public InterpretationThinConferenceActionCallback LeaveConferenceCallback { get; set;}
		public InterpretationThinConferenceActionCallback EndConferenceCallback { get; set; }
		public InterpretationThinConferenceActionCallback HoldCallback { get; set; }
		public InterpretationThinConferenceActionCallback ResumeCallback { get; set; }
		public InterpretationThinConferenceActionCallback StartRecordingCallback { get; set; }
		public InterpretationThinConferenceActionCallback StopRecordingCallback { get; set; }
		public InterpretationThinConferenceActionCallback PauseRecordingCallback { get; set; }
		public InterpretationThinConferenceDtmfCallback SendDtmfCallback { get; set; }


		public InterpretationThinConference(ThinParticipant participant)
		{
			m_Participant = new KeyValuePair<ParticipantInfo, ThinParticipant>(new ParticipantInfo(participant.Name, participant.Number), participant);
		}

		/// <summary>
		/// Gets the participants in this conference.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ThinParticipant> GetParticipants()
		{
			yield return m_Participant.Value;
		}

		/// <summary>
		/// Leaves the conference, keeping the conference in tact for other participants.
		/// </summary>
		public override void LeaveConference()
		{
			var handler = LeaveConferenceCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Ends the conference for all participants.
		/// </summary>
		public override void EndConference()
		{
			var handler = EndConferenceCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Holds the conference
		/// </summary>
		public override void Hold()
		{
			var handler = HoldCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Resumes the conference
		/// </summary>
		public override void Resume()
		{
			var handler = ResumeCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Sends DTMF to the participant.
		/// </summary>
		/// <param name="data"></param>
		public override void SendDtmf(string data)
		{
			var handler = SendDtmfCallback;
			if (handler != null)
				handler(this, data);
		}

		/// <summary>
		/// Starts recording the conference.
		/// </summary>
		public override void StartRecordingConference()
		{
			var handler = StartRecordingCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Stops recording the conference.
		/// </summary>
		public override void StopRecordingConference()
		{
			var handler = StopRecordingCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Pauses the current recording of the conference.
		/// </summary>
		public override void PauseRecordingConference()
		{
			var handler = PauseRecordingCallback;
			if (handler != null)
				handler(this);
		}

		public void UpdateFromConferenceState([NotNull] ConferenceState state)
		{
			if (state == null)
				throw new ArgumentNullException("state");

			SupportedConferenceFeatures = state.SupportedConferenceFeatures;
			Name = state.Name;
			Status = state.Status;
			StartTime = state.StartTime;
			EndTime = state.EndTime;
			CallType = state.CallType;
			RecordingStatus = state.RecordingStatus;

			var stateParticipant = new KeyValuePair<ParticipantInfo, ParticipantState>(new ParticipantInfo(state.ParticipantStates), state.ParticipantStates);

			// No change
			if (ParticipantInfoEqualityComparer.Instance.Equals(m_Participant.Key, stateParticipant.Key))
				return;

			// Removed
			var removed = m_Participant.Value;
			OnParticipantRemoved.Raise(this, removed);

			// Added
			var added = stateParticipant.Value.ToThinParticipant();
			OnParticipantAdded.Raise(this, added);

			m_Participant = new KeyValuePair<ParticipantInfo, ThinParticipant>(stateParticipant.Key, added);
		}

		/// <summary>
		/// Makes a new InterpretationThinConference from the given conference state, including participants
		/// </summary>
		/// <param name="state"></param>
		/// <returns></returns>
		public static InterpretationThinConference FromConferenceState([NotNull] ConferenceState state)
		{
			if (state == null)
				throw new ArgumentNullException("state");

			var conference = new InterpretationThinConference(state.ParticipantStates.ToThinParticipant())
			{
				SupportedConferenceFeatures = state.SupportedConferenceFeatures,
				Name = state.Name,
				Number = state.Number,
				Status = state.Status,
				StartTime = state.StartTime,
				EndTime = state.EndTime,
				CallType = state.CallType,
				RecordingStatus = state.RecordingStatus,
			};

			return conference;
		}

		
	}
}