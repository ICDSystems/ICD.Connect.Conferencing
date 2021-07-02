using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference
{
	/// <summary>
	/// Call Type
	/// </summary>
	public enum eCiscoCallType
	{
		// Ignore missing comments warning
#pragma warning disable 1591
		Unknown,
		Video,
		Audio,
		AudioCanEscalate,
		ForwardAllCall
#pragma warning restore 1591
	}

	public static class CiscoCallTypeExtensions
	{
		public static eCallType ToCallType(this eCiscoCallType callType)
		{
			switch (callType)
			{
				case eCiscoCallType.Video:
					return eCallType.Video;

				case eCiscoCallType.Audio:
				case eCiscoCallType.AudioCanEscalate:
				case eCiscoCallType.ForwardAllCall:
					return eCallType.Audio;

				case eCiscoCallType.Unknown:
					return eCallType.Unknown;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}

	public sealed class CiscoConference : AbstractConference<CiscoParticipant>, ICiscoConference
	{
		private readonly DialingComponent m_DialingComponent;
		private readonly CallStatus m_CallStatus;

		private CiscoParticipant m_Participant;

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="dialingComponent"></param>
		/// <param name="callStatus"></param>
		public CiscoConference([NotNull] DialingComponent dialingComponent, CallStatus callStatus)
		{
			if (dialingComponent == null)
				throw new ArgumentNullException("dialingComponent");

			m_DialingComponent = dialingComponent;
			m_CallStatus = callStatus;

			UpdateCallStatus(m_CallStatus);

			Subscribe(m_CallStatus);

			SupportedConferenceFeatures = eConferenceFeatures.EndConference;
		}

		#endregion

		#region Methods

		private void UpdateCallStatus(CallStatus callStatus)
		{
			if (callStatus == null)
				return;

			Name = callStatus.Name;
			Number = callStatus.Number;
			Direction = callStatus.Direction;
			AnswerState = callStatus.AnswerState;
			Status = callStatus.Status.ToConferenceStatus();
		}

		public void InitializeConference()
		{
			m_Participant = new CiscoParticipant(m_CallStatus, m_DialingComponent);
			Subscribe(m_Participant);
			AddParticipant(m_Participant);
		}

		protected override void DisposeFinal()
		{
			Unsubscribe(m_CallStatus);
			Unsubscribe(m_Participant);

			base.DisposeFinal();
		}

		public override void EndConference()
		{
			m_DialingComponent.Hangup(m_CallStatus);
		}

		/// <summary>
		/// Holds the conference
		/// </summary>
		public override void Hold()
		{
			m_DialingComponent.Hold(m_CallStatus);
		}

		/// <summary>
		/// Resumes the conference
		/// </summary>
		public override void Resume()
		{
			m_DialingComponent.Resume(m_CallStatus);
		}

		/// <summary>
		/// Sends DTMF to the participant.
		/// </summary>
		/// <param name="data"></param>
		public override void SendDtmf(string data)
		{
			m_DialingComponent.SendDtmf(m_CallStatus, data);
		}

		public override void StartRecordingConference()
		{
			throw new NotSupportedException();
		}

		public override void StopRecordingConference()
		{
			throw new NotSupportedException();
		}

		public override void PauseRecordingConference()
		{
			throw new NotSupportedException();
		}

		public override void LeaveConference()
		{
			throw new NotSupportedException();
		}


		#endregion

		#region CallStatus Callback

		private void Subscribe(CallStatus callStatus)
		{
			if (callStatus == null)
				return;

			callStatus.OnNameChanged += CallStatusOnNameChanged;
			callStatus.OnNumberChanged += CallStatusOnNumberChanged;
			callStatus.OnDirectionChanged += CallStatusOnDirectionChanged;
			callStatus.OnAnswerStateChanged += CallStatusOnAnswerStateChanged;
			callStatus.OnStatusChanged += CallStatusOnStatusChanged;
		}

		private void Unsubscribe(CallStatus callStatus)
		{
			if (callStatus == null)
				return;

			callStatus.OnNameChanged -= CallStatusOnNameChanged;
			callStatus.OnNumberChanged -= CallStatusOnNumberChanged;
			callStatus.OnDirectionChanged -= CallStatusOnDirectionChanged;
			callStatus.OnAnswerStateChanged -= CallStatusOnAnswerStateChanged;
			callStatus.OnStatusChanged -= CallStatusOnStatusChanged;
		}

		private void CallStatusOnNameChanged(object sender, StringEventArgs args)
		{
			Name = args.Data;
		}

		private void CallStatusOnNumberChanged(object sender, StringEventArgs args)
		{
			Number = args.Data;
		}

		private void CallStatusOnDirectionChanged(object sender, GenericEventArgs<eCallDirection> args)
		{
			Direction = args.Data;
		}

		private void CallStatusOnAnswerStateChanged(object sender, GenericEventArgs<eCallAnswerState> args)
		{
			AnswerState = args.Data;
		}

		private void CallStatusOnStatusChanged(object sender, GenericEventArgs<eParticipantStatus> args)
		{
			Status = args.Data.ToConferenceStatus();
		}

		#endregion

		#region Participant Callbacks

		private void Subscribe(CiscoParticipant participant)
		{
			participant.OnStatusChanged += ParticipantOnStatusChanged;
		}

		private void Unsubscribe(CiscoParticipant participant)
		{
			participant.OnStatusChanged -= ParticipantOnStatusChanged;
		}

		private void ParticipantOnStatusChanged(object sender, ParticipantStatusEventArgs args)
		{
			Status = args.Data.ToConferenceStatus();
		}

		#endregion
	}
}