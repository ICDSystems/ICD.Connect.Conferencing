using System;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Participants
{
	/// <summary>
	/// A participant represents a conference participant
	/// </summary>
	public interface IParticipant : IConsoleNode, IDisposable
	{
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

			switch (extends.Status)
			{
				case eParticipantStatus.Undefined:
				case eParticipantStatus.Dialing:
				case eParticipantStatus.Connecting:
				case eParticipantStatus.Ringing:
				case eParticipantStatus.Disconnecting:
				case eParticipantStatus.Disconnected:
				case eParticipantStatus.Idle:
					return false;

				case eParticipantStatus.Connected:
				case eParticipantStatus.OnHold:
				case eParticipantStatus.EarlyMedia:
				case eParticipantStatus.Preserved:
				case eParticipantStatus.RemotePreserved:
					return true;

				default:
					throw new ArgumentOutOfRangeException();
			}
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
	}
}