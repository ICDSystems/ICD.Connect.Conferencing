using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Conferences
{
	#region Delegates

	public delegate void ThinConferenceActionCallback(ThinConference sender);
	public delegate void ThinConferenceDtmfCallback(ThinConference sender, string data);

	#endregion

	public sealed class ThinConference : AbstractConferenceBase<ThinParticipant>, IDisposable
	{
		#region Events

		public override event EventHandler<ParticipantEventArgs> OnParticipantAdded;

		public override event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		#endregion

		#region Fields

		#region Callbacks

		private ThinConferenceDtmfCallback m_SendDtmfCallback;
		private ThinConferenceActionCallback m_LeaveConferenceCallback;
		private ThinConferenceActionCallback m_EndConferenceCallback;
		private ThinConferenceActionCallback m_StartRecordingCallback;
		private ThinConferenceActionCallback m_StopRecordingCallback;
		private ThinConferenceActionCallback m_PauseRecordingCallback;
		private ThinConferenceActionCallback m_HoldCallback;
		private ThinConferenceActionCallback m_ResumeCallback;

		#endregion

		private readonly Participants.ThinParticipant m_Participant;

		#endregion

		#region Properties

		public Participants.ThinParticipant Participant {get { return m_Participant; }}

		public new eConferenceStatus Status
		{
			get { return base.Status; }
			set { base.Status = value; }
		}

		public new string Name
		{
			get { return base.Name; }
			set
			{
				base.Name = value;
				Participant.Name = value;
			}
		}

		/// <summary>
		/// Gets the type of call.
		/// </summary>
		public new eCallType CallType
		{
			get { return base.CallType; }
			set
			{
				base.CallType = value;
				Participant.CallType = value;
			}
		}

		public new DateTime? StartTime
		{
			get { return Participant.StartTime; }
			set
			{
				Participant.StartTime = value;
				base.StartTime = value;
			}
		}

		public new DateTime? EndTime
		{
			get { return Participant.EndTime; }
			set
			{
				Participant.EndTime = value;
				base.EndTime = value;
			}
		}

		public string Number
		{
			get { return Participant.Number; }
			set
			{
				Participant.Number = value;
				if (string.IsNullOrEmpty(Name))
					Name = value;
			}
		}

		public eParticipantStatus ParticipantStatus
		{
			get { return Participant.Status; }
			set { Participant.Status = value; }
		}

		public eCallDirection Direction
		{
			get { return Participant.Direction; }
			set { Participant.Direction = value; }
		}

		public DateTime DialTime
		{
			get { return Participant.DialTime; }
			set { Participant.DialTime = value; }
		}

		public eCallAnswerState AnswerState
		{
			get { return Participant.AnswerState; }
			set { Participant.AnswerState = value; }
		}

		#region Callbacks

		public ThinConferenceActionCallback LeaveConferenceCallback
		{
			get { return m_LeaveConferenceCallback; }
			set
			{
				m_LeaveConferenceCallback = value;

				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.LeaveConference,
				                                                                   m_LeaveConferenceCallback != null);
			}
		}

		public ThinConferenceActionCallback EndConferenceCallback
		{
			get { return m_EndConferenceCallback; }
			set
			{
				m_EndConferenceCallback = value;

				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.EndConference,
				                                                                   m_EndConferenceCallback != null);
			}
		}

		public ThinConferenceActionCallback StartRecordingCallback
		{
			get { return m_StartRecordingCallback; }
			set
			{
				m_StartRecordingCallback = value;
				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.StartRecording,
				                                                                   m_StartRecordingCallback != null);
			}
		}

		public ThinConferenceActionCallback StopRecordingCallback
		{
			get { return m_StopRecordingCallback; }
			set
			{
				m_StopRecordingCallback = value;
				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.StopRecording,
				                                                                   m_StopRecordingCallback != null);
			}
		}

		public ThinConferenceActionCallback PauseRecordingCallback
		{
			get { return m_PauseRecordingCallback; }
			set
			{
				m_PauseRecordingCallback = value;
				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.PauseRecording,
				                                                                   m_PauseRecordingCallback != null);
			}
		}

		public ThinConferenceActionCallback HoldCallback
		{
			get { return m_HoldCallback; }
			set
			{
				m_HoldCallback = value;
				UpdateConferenceHoldFeature();
			}
		}

		public ThinConferenceActionCallback ResumeCallback
		{
			get { return m_ResumeCallback; }
			set
			{
				m_ResumeCallback = value;
				UpdateConferenceHoldFeature();
			}
		}

		public ThinConferenceDtmfCallback SendDtmfCallback
		{
			get { return m_SendDtmfCallback; }
			set
			{
				m_SendDtmfCallback = value;

				SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.SendDtmf, m_SendDtmfCallback != null);
			}
		}

		#endregion

		#endregion

		#region Constructor

		public ThinConference()
		{
			m_Participant = new Participants.ThinParticipant();
			StartTime = DateTime.UtcNow;
		}

		private ThinConference(Participants.ThinParticipant participant)
		{
			m_Participant = participant;
			StartTime = m_Participant.StartTime ?? DateTime.UtcNow;
		}

		#endregion

		#region Public Methods

		protected override void DisposeFinal()
		{
			LeaveConferenceCallback = null;
			EndConferenceCallback = null;
			StartRecordingCallback = null;
			StopRecordingCallback = null;
			PauseRecordingCallback = null;
			HoldCallback = null;
			ResumeCallback = null;
			SendDtmfCallback = null;

			base.DisposeFinal();
		}

		/// <summary>
		/// Gets the participants in this conference.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<Participants.ThinParticipant> GetParticipants()
		{
			yield return m_Participant;
		}

		public override void LeaveConference()
		{
			var handler = LeaveConferenceCallback;
			if (handler != null)
				handler(this);
		}

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

		public override void StartRecordingConference()
		{
			var handler = StartRecordingCallback;
			if (handler != null)
				handler(this);
		}

		public override void StopRecordingConference()
		{
			var handler = StopRecordingCallback;
			if (handler != null)
				handler(this);
		}

		public override void PauseRecordingConference()
		{
			var handler = PauseRecordingCallback;
			if (handler != null)
				handler(this);
		}

		#endregion

		#region Private Methods

		private void UpdateConferenceHoldFeature()
		{
			bool holdSupported = HoldCallback != null && ResumeCallback != null;
			SupportedConferenceFeatures = SupportedConferenceFeatures.SetFlags(eConferenceFeatures.Hold, holdSupported);
		}

		#endregion

		#region Static Methods

		/// <summary>
		/// Generates a new ThinParticipant based on an incoming call
		/// </summary>
		/// <param name="incomingCall"></param>
		/// <returns></returns>
		private static Participants.ThinParticipant ParticipantFromIncomingCall([NotNull] IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				throw new ArgumentNullException("incomingCall");

			return new Participants.ThinParticipant
			{
				DialTime = incomingCall.StartTime,
				Name = incomingCall.Name,
				Number = incomingCall.Number,
				AnswerState = incomingCall.AnswerState,
				Direction = eCallDirection.Incoming,
				StartTime = incomingCall.StartTime
			};
		}

		public static ThinConference FromIncomingCall([NotNull] IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				throw new ArgumentNullException("incomingCall");

			return new ThinConference(ParticipantFromIncomingCall(incomingCall))
			{
				Name = incomingCall.Name ?? incomingCall.Number,
				StartTime = incomingCall.StartTime
			};
		}

		#endregion
	}
}
