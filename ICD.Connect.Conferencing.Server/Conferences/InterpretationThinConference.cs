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


		private readonly Dictionary<ParticipantInfo, ThinParticipant> m_Participants;
		private readonly SafeCriticalSection m_ParticipantsSection;

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

		public InterpretationThinConference()
		{
			m_Participants = new Dictionary<ParticipantInfo, ThinParticipant>(ParticipantInfoEqualityComparer.Instance);
			m_ParticipantsSection = new SafeCriticalSection();
		}

		public InterpretationThinConference(IEnumerable<ThinParticipant> participants):this()
		{
			m_Participants.AddRange(participants, p => new ParticipantInfo(p.Name, p.Number));
		}

		/// <summary>
		/// Gets the participants in this conference.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ThinParticipant> GetParticipants()
		{
			return m_ParticipantsSection.Execute(() => m_Participants.Values.ToArray(m_Participants.Count));
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

			var stateParticipants = new Dictionary<ParticipantInfo, ParticipantState>();
			stateParticipants.AddRange(state.ParticipantStates, p => new ParticipantInfo(p));

			//Used to update things outside the ParticipantsSection for thread safety
			List<ThinParticipant> participantsToUpdate;
			List<ThinParticipant> newParticipants = new List<ThinParticipant>();
			List<ThinParticipant> removedParticipants = new List<ThinParticipant>();


			m_ParticipantsSection.Enter();
			try
			{
				// Find new and removed participants
				List<ParticipantInfo> newParticipantInfos = stateParticipants.Keys.Except(m_Participants.Keys).ToList();
				List<ParticipantInfo> removedParticipantInfos = m_Participants.Keys.Except(stateParticipants.Keys).ToList();
				
				// Drop removed participants
				foreach (var participantInfo in removedParticipantInfos)
				{
					removedParticipants.Add(m_Participants[participantInfo]);
					m_Participants.Remove(participantInfo);
				}

				// Get list to update so we can update outside the critical section
				participantsToUpdate = m_Participants.Values.ToList(m_Participants.Count);

				// Add new participants
				foreach (var participantInfo in newParticipantInfos)
				{
					var participant = stateParticipants[participantInfo].ToThinParticipant();
					newParticipants.Add(participant);
					m_Participants.Add(participantInfo, participant);
				}
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}

			//Fire removed event
			foreach (var participant in removedParticipants)
			{
				OnParticipantRemoved.Raise(this, participant);
				participant.Dispose();
			}

			// Update existing participants
			foreach (var participant in participantsToUpdate)
			{
				ParticipantState updatedState;
				if (stateParticipants.TryGetValue(new ParticipantInfo(participant), out updatedState))
					participant.UpdateFromParticipantState(updatedState);
			}

			//Fire added event
			foreach (var participant in newParticipants)
			{
				OnParticipantAdded.Raise(this, participant);
			}
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

			var conference = new InterpretationThinConference(state.ParticipantStates.Select(s => s.ToThinParticipant()))
			{
				SupportedConferenceFeatures = state.SupportedConferenceFeatures,
				Name = state.Name,
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