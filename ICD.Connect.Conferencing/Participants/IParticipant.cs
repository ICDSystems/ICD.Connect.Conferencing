using System;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.Participants
{
	[Flags]
	public enum eParticipantFeatures
	{
		None = 0,

		GetName = 1,

		GetCallType = 2,

		GetCamera = 4,

		GetStatus = 8,

		GetStartTime = 16,

		GetEndTime = 32,

		GetNumber = 64,

		GetDirection = 128,

		GetDialTime = 256,

		GetAnswerState = 512,

		GetIsMuted = 1024,

		GetIsSelf = 2048,

		GetIsHost = 4096,

		Holding = 8192,

		Hangup = 16384,

		SendDtmf = 32768,

		Kick = 65536,

		SetMute = 131072,

		RaiseLowerHand = 262144,

		Record = 524288,

		Admit = 1048576
	}


	/// <summary>
	/// A participant represents a conference participant
	/// </summary>
	public interface IParticipant : IConsoleNode, IDisposable
	{
		#region Events

		/// <summary>
		/// Raised when the participant's status changes.
		/// </summary>
		event EventHandler<ParticipantStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the participant source type changes.
		/// </summary>
		event EventHandler<CallTypeEventArgs> OnParticipantTypeChanged;

		/// <summary>
		/// Raised when the participant's name changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the participant's start time changes
		/// </summary>
		event EventHandler<DateTimeNullableEventArgs> OnStartTimeChanged;

		/// <summary>
		/// Raised when the participant's end time changes
		/// </summary>
		event EventHandler<DateTimeNullableEventArgs> OnEndTimeChanged;

		/// <summary>
		/// Raised when the source number changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNumberChanged;

		/// <summary>
		/// Raised when the participant is answered, dismissed or ignored.
		/// </summary>
		event EventHandler<CallAnswerStateEventArgs> OnAnswerStateChanged;

		/// <summary>
		/// Raised when the participant's mute status changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnIsMutedChanged;

		/// <summary>
		/// Raised when the participant's host status changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnIsHostChanged;

		/// <summary>
		/// Raised when the participant's virtual hand raised state changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnHandRaisedChanged;

		/// <summary>
		/// Raised when the participant's ability to record calls changes.
		/// </summary>
		event EventHandler<BoolEventArgs> OnCanRecordChanged;

		/// <summary>
		/// Raised when the participant starts/stops recording a call.
		/// </summary>
		event EventHandler<BoolEventArgs> OnIsRecordingChanged;

		/// <summary>
		/// Raised when the supported participant features changes.
		/// </summary>
		event EventHandler<ConferenceParticipantSupportedFeaturesChangedApiEventArgs> OnSupportedParticipantFeaturesChanged; 

		#endregion

		#region Properties

		/// <summary>
		/// Gets the source name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the participant's source type.
		/// </summary>
		eCallType CallType { get; }

		/// <summary>
		/// Gets the remote camera.
		/// </summary>
		IRemoteCamera Camera { get; }

		/// <summary>
		/// Gets the participant's status (Idle, Dialing, Ringing, etc)
		/// </summary>
		eParticipantStatus Status { get; }

		/// <summary>
		/// The time when participant connected.
		/// </summary>
		DateTime? StartTime { get; }

		/// <summary>
		/// The time when participant disconnected.
		/// </summary>
		DateTime? EndTime { get; }

		/// <summary>
		/// Gets the number of the participant
		/// </summary>
		string Number { get; }

		/// <summary>
		/// Call Direction
		/// </summary>
		eCallDirection Direction { get; }

		/// <summary>
		/// Gets the time the call was dialed.
		/// </summary>
		DateTime DialTime { get; }

		/// <summary>
		/// Returns the answer state for the participant.
		/// Note, in order for a participant to exist, the call must be answered, so this value will be either answered or auto-answered always.
		/// </summary>
		eCallAnswerState AnswerState { get; }

		/// <summary>
		/// Whether or not the participant is muted.
		/// </summary>
		bool IsMuted { get; }

		/// <summary>
		/// Whether or not the participant is the room itself.
		/// </summary>
		bool IsSelf { get; }

		/// <summary>
		/// Whether or not the participant is the meeting host.
		/// </summary>
		bool IsHost { get; }

		/// <summary>
		/// Whether or not the participant's virtual hand is raised.
		/// </summary>
		bool HandRaised { get; }

		/// <summary>
		/// Whether or not the participant is currently allowed to record the call.
		/// </summary>
		bool CanRecord { get; }

		/// <summary>
		/// Whether or not the participant is actively recording the call.
		/// </summary>
		bool IsRecording { get; }

		/// <summary>
		/// The features supported by the participant.
		/// </summary>
		eParticipantFeatures SupportedParticipantFeatures { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Holds the participant.
		/// </summary>
		void Hold();

		/// <summary>
		/// Resumes the participant.
		/// </summary>
		void Resume();

		/// <summary>
		/// Disconnects the participant.
		/// </summary>
		void Hangup();

		/// <summary>
		/// Sends DTMF to the participant.
		/// </summary>
		/// <param name="data"></param>
		void SendDtmf(string data);

		/// <summary>
		/// Admits the participant into the conference.
		/// </summary>
		void Admit();

		/// <summary>
		/// Kick the participant from the conference.
		/// </summary>
		/// <returns></returns>
		void Kick();

		/// <summary>
		/// Mute the participant in the conference.
		/// </summary>
		/// <returns></returns>
		void Mute(bool mute);

		/// <summary>
		/// Raises/Lowers the participant's virtual hand.
		/// </summary>
		void ToggleHandRaise();

		/// <summary>
		/// Based on the current call record state, does the next call record action.
		/// </summary>
		/// <param name="stop">If true stops any active recording</param>
		void RecordCallAction(bool stop);

		#endregion
	}

	public static class ParticipantExtensions
	{
		/// <summary>
		/// Gets the duration of the call.
		/// </summary>
		/// <param name="extends"></param>
		public static TimeSpan GetDuration(this IParticipant extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (extends.StartTime == null)
				return new TimeSpan();

			DateTime end = (extends.EndTime != null) ? (DateTime)extends.EndTime : IcdEnvironment.GetUtcTime();

			return end - (DateTime)extends.StartTime;
		}

		/// <summary>
		/// Returns true if the source is connected.
		/// </summary>
		/// <param name="extends"></param>
		public static bool GetIsOnline(this IParticipant extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return extends.Status.GetIsOnline();
		}

		/// <summary>
		/// Returns true if the source is active.
		/// </summary>
		/// <param name="extends"></param>
		public static bool GetIsActive(this IParticipant extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			switch (extends.Status)
			{
				case eParticipantStatus.Undefined:
				case eParticipantStatus.Disconnected:
				case eParticipantStatus.Idle:
					return false;

				case eParticipantStatus.Waiting:
				case eParticipantStatus.Connected:
				case eParticipantStatus.OnHold:
				case eParticipantStatus.EarlyMedia:
				case eParticipantStatus.Preserved:
				case eParticipantStatus.RemotePreserved:
				case eParticipantStatus.Dialing:
				case eParticipantStatus.Connecting:
				case eParticipantStatus.Ringing:
				case eParticipantStatus.Disconnecting:
					return true;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Allows sending data to dial-tone menus.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="data"></param>
		public static void SendDtmf(this IParticipant extends, char data)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			extends.SendDtmf(data.ToString());
		}

		/// <summary>
		/// Gets the start time, falls through to dial time if no start time specified.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static DateTime GetStartOrDialTime(this IParticipant extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return extends.StartTime ?? extends.DialTime;
		}
	}
}