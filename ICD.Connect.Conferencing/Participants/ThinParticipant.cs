using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Participants
{
	public delegate void ThinParticipantHoldCallback(ThinParticipant sender);

	public delegate void ThinParticipantResumeCallback(ThinParticipant sender);

	public delegate void ThinParticipantSendDtmfCallback(ThinParticipant sender, string data);

	public delegate void ThinParticipantHangupCallback(ThinParticipant sender);

	public delegate void ThinParticipantKickCallback(ThinParticipant sender);

	public delegate void ThinParticipantMuteCallback(ThinParticipant sender, bool mute);

	public delegate void ThinParticipantToggleHandRaiseCallback(ThinParticipant sender);

	public delegate void ThinParticipantAdmitCallback(ThinParticipant sender);

	public sealed class ThinParticipant : AbstractParticipant
	{
		#region Properties

		public ThinParticipantHoldCallback HoldCallback { get; set; }
		public ThinParticipantResumeCallback ResumeCallback { get; set; }
		public ThinParticipantSendDtmfCallback SendDtmfCallback { get; set; }
		public ThinParticipantHangupCallback HangupCallback { get; set; }
		public ThinParticipantKickCallback KickCallback { get; set; }
		public ThinParticipantMuteCallback MuteCallback { get; set; }
		public ThinParticipantToggleHandRaiseCallback HandRaiseCallback { get; set; }
		public ThinParticipantAdmitCallback AdmitCallback { get; set; }

		public override IRemoteCamera Camera { get { return null; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ThinParticipant()
		{
			DialTime = IcdEnvironment.GetUtcTime();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal()
		{
			HoldCallback = null;
			ResumeCallback = null;
			SendDtmfCallback = null;
			HangupCallback = null;
			KickCallback = null;
			MuteCallback = null;
			HandRaiseCallback = null;
			AdmitCallback = null;

			base.DisposeFinal();
		}

		#region Methods

		/// <summary>
		/// Holds the source.
		/// </summary>
		public override void Hold()
		{
			ThinParticipantHoldCallback handler = HoldCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Resumes the source.
		/// </summary>
		public override void Resume()
		{
			ThinParticipantResumeCallback handler = ResumeCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Sends DTMF to the source.
		/// </summary>
		/// <param name="data"></param>
		public override void SendDtmf(string data)
		{
			ThinParticipantSendDtmfCallback handler = SendDtmfCallback;
			if (handler != null)
				handler(this, data ?? string.Empty);
		}

		/// <summary>
		/// Kick the participant from the conference.
		/// </summary>
		/// <returns></returns>
		public override void Kick()
		{
			ThinParticipantKickCallback handler = KickCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Mute the participant in the conference.
		/// </summary>
		/// <returns></returns>
		public override void Mute(bool mute)
		{
			ThinParticipantMuteCallback handler = MuteCallback;
			if (handler != null)
				handler(this, mute);
		}

		/// <summary>
		/// Raises/Lowers the participant's virtual hand.
		/// </summary>
		public override void ToggleHandRaise()
		{
			ThinParticipantToggleHandRaiseCallback handler = HandRaiseCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Disconnects the source.
		/// </summary>
		public override void Hangup()
		{
			ThinParticipantHangupCallback handler = HangupCallback;
			if (handler != null)
				handler(this);
		}

		/// <summary>
		/// Admits the participant into the conference.
		/// </summary>
		public override void Admit()
		{
			ThinParticipantAdmitCallback handler = AdmitCallback;
			if (handler != null)
				handler(this);
		}

		#endregion

		#region Property Setters

		public void SetName(string callerName)
		{
			Name = callerName;
		}

		public void SetNumber(string callerNumber)
		{
			Number = callerNumber;
		}

		public void SetStatus(eParticipantStatus status)
		{
			Status = status;
		}

		public void SetDirection(eCallDirection direction)
		{
			Direction = direction;
		}

		public void SetCallType(eCallType type)
		{
			CallType = type;
		}

		public void SetStart(DateTime start)
		{
			StartTime = start;
		}

		public void SetEnd(DateTime end)
		{
			EndTime = end;
		}

		public void SetDialTime(DateTime dialTime)
		{
			DialTime = dialTime;
		}

		public void SetAnswerState(eCallAnswerState answerState)
		{
			AnswerState = answerState;
		}

		#endregion

		#region Static Methods

		/// <summary>
		/// Generates a new ThinParticipant based on an incoming call
		/// </summary>
		/// <param name="incomingCall"></param>
		/// <returns></returns>
		public static ThinParticipant FromIncomingCall([NotNull] IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				throw new ArgumentNullException("incomingCall");

			return new ThinParticipant
			{
				DialTime = incomingCall.StartTime,
				Name = incomingCall.Name,
				Number = incomingCall.Number,
				AnswerState = incomingCall.AnswerState,
				Direction = eCallDirection.Incoming
			};
		}

		#endregion
	}
}