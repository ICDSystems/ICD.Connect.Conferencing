using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;

namespace ICD.Connect.Conferencing.Participants
{
	public delegate void ThinParticipantHoldCallback(ThinTraditionalParticipant sender);

	public delegate void ThinParticipantResumeCallback(ThinTraditionalParticipant sender);

	public delegate void ThinParticipantSendDtmfCallback(ThinTraditionalParticipant sender, string data);

	public delegate void ThinParticipantHangupCallback(ThinTraditionalParticipant sender);

	public sealed class ThinTraditionalParticipant : AbstractTraditionalParticipant
	{
		#region Properties

		public ThinParticipantHoldCallback HoldCallback { get; set; }
		public ThinParticipantResumeCallback ResumeCallback { get; set; }
		public ThinParticipantSendDtmfCallback SendDtmfCallback { get; set; }
		public ThinParticipantHangupCallback HangupCallback { get; set; }

		public override IRemoteCamera Camera { get { return null; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ThinTraditionalParticipant()
		{
			DialTime = IcdEnvironment.GetUtcTime();
		}

		/// <summary>
		/// Creates a new thin participant based on an incoming call
		/// </summary>
		/// <param name="incomingCall"></param>
		public ThinTraditionalParticipant([NotNull] IIncomingCall incomingCall)
		{
			if (incomingCall == null)
				throw new ArgumentNullException("incomingCall");

			DialTime = incomingCall.StartTime;
			Name = incomingCall.Name;
			Number = incomingCall.Number;
			AnswerState = incomingCall.AnswerState;
			Direction = eCallDirection.Incoming;
			DialTime = incomingCall.StartTime;
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
		/// Disconnects the source.
		/// </summary>
		public override void Hangup()
		{
			ThinParticipantHangupCallback handler = HangupCallback;
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
	}
}
