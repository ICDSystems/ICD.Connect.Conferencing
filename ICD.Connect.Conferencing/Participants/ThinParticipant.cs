using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Participants
{
	public delegate void ThinParticipantKickCallback(ThinParticipant sender);

	public delegate void ThinParticipantMuteCallback(ThinParticipant sender, bool mute);

	public delegate void ThinParticipantSetHandPositionCallback(ThinParticipant sender, bool raised);

	public delegate void ThinParticipantAdmitCallback(ThinParticipant sender);

	public sealed class ThinParticipant : AbstractParticipant
	{
		#region Properties

		public ThinParticipantKickCallback KickCallback { get; set; }
		public ThinParticipantMuteCallback MuteCallback { get; set; }
		public ThinParticipantSetHandPositionCallback HandPositionCallback { get; set; }
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
			KickCallback = null;
			MuteCallback = null;
			HandPositionCallback = null;
			AdmitCallback = null;

			base.DisposeFinal();
		}

		#region Methods

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
		public override void SetHandPosition(bool raised)
		{
			ThinParticipantSetHandPositionCallback handler = HandPositionCallback;
			if (handler != null)
				handler(this, raised);
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