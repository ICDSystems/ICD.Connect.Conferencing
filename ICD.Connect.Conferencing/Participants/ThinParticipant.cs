using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Participants
{
	public delegate void ThinParticipantActionCallback(ThinParticipant sender);

	public delegate void ThinParticipantMuteCallback(ThinParticipant sender, bool mute);

	public delegate void ThinParticipantSetHandPositionCallback(ThinParticipant sender, bool raised);

	public sealed class ThinParticipant : AbstractParticipant
	{
		#region Fields

		private ThinParticipantActionCallback m_KickCallback;
		private ThinParticipantMuteCallback m_MuteCallback;
		private ThinParticipantSetHandPositionCallback m_HandPositionCallback;
		private ThinParticipantActionCallback m_AdmitCallback;

		#endregion

		#region Properties

		public ThinParticipantActionCallback KickCallback
		{
			get { return m_KickCallback; }
			set
			{
				m_KickCallback = value;
				SupportedParticipantFeatures = SupportedParticipantFeatures.SetFlags(eParticipantFeatures.Kick,
				                                                                     m_KickCallback != null);
			}
		}

		public ThinParticipantMuteCallback MuteCallback
		{
			get { return m_MuteCallback; }
			set
			{
				m_MuteCallback = value;
				SupportedParticipantFeatures = SupportedParticipantFeatures.SetFlags(eParticipantFeatures.SetMute,
				                                                                     m_MuteCallback != null);
			}
		}

		public ThinParticipantSetHandPositionCallback HandPositionCallback
		{
			get { return m_HandPositionCallback; }
			set
			{
				m_HandPositionCallback = value;
				SupportedParticipantFeatures = SupportedParticipantFeatures.SetFlags(eParticipantFeatures.RaiseLowerHand,
				                                                                     m_HandPositionCallback != null);
			}
		}

		public ThinParticipantActionCallback AdmitCallback
		{
			get { return m_AdmitCallback; }
			set
			{
				m_AdmitCallback = value;
				SupportedParticipantFeatures = SupportedParticipantFeatures.SetFlags(eParticipantFeatures.Admit,
				                                                                     m_AdmitCallback != null);
			}
		}

		public override IRemoteCamera Camera { get { return null; } }

		public new eCallType CallType
		{
			get { return base.CallType; }
			set { base.CallType = value; }
		}

		public new string Name
		{
			get { return base.Name; }
			set { base.Name = value; }
		}

		public new string Number
		{
			get { return base.Number; }
			set { base.Number = value; }
		}

		public new eParticipantStatus Status
		{
			get { return base.Status; }
			set { base.Status = value; }
		}

		public new eCallDirection Direction
		{
			get { return base.Direction; }
			set { base.Direction = value; }
		}

		public new DateTime? StartTime
		{
			get { return base.StartTime; }
			set { base.StartTime = value; }
		}

		public new DateTime? EndTime
		{
			get { return base.EndTime; } 
			set { base.EndTime = value; }
		}

		public new DateTime DialTime
		{
			get { return base.DialTime; }
			set { base.DialTime = value; }
		}

		public new eCallAnswerState AnswerState
		{
			get { return base.AnswerState; }
			set { base.AnswerState = value; }
		}

		public new bool IsMuted
		{
			get { return base.IsMuted; }
			set { base.IsMuted = value; }
		}

		public new bool IsHost
		{
			get { return base.IsHost; }
			set { base.IsHost = value; }
		}

		public new bool IsSelf
		{
			get { return base.IsSelf; }
			set { base.IsSelf = value; }
		}

		public new eParticipantFeatures SupportedParticipantFeatures
		{
			get { return base.SupportedParticipantFeatures; }
			set { base.SupportedParticipantFeatures = value; }
		}

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
			ThinParticipantActionCallback handler = KickCallback;
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
			ThinParticipantActionCallback handler = AdmitCallback;
			if (handler != null)
				handler(this);
		}

		#endregion
	}
}