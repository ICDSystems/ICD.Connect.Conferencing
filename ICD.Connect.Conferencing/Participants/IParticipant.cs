using System;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Participants
{
	/// <summary>
	/// A participant represents a conference participant
	/// </summary>
	public interface IParticipant : IConsoleNode
	{
		/// <summary>
		/// Raised when the participant's status changes.
		/// </summary>
		event EventHandler<ParticipantStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the participant source type changes.
		/// </summary>
		event EventHandler<ParticipantTypeEventArgs> OnParticipantTypeChanged;

		/// <summary>
		/// Raised when the participant's name changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Gets the source name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the participant's source type.
		/// </summary>
		eCallType CallType { get; }

		/// <summary>
		/// Gets the participant's status (Idle, Dialing, Ringing, etc)
		/// </summary>
		eParticipantStatus Status { get; }

		/// <summary>
		/// The time when participant connected.
		/// </summary>
		DateTime? Start { get; }

		/// <summary>
		/// The time when participant disconnected.
		/// </summary>
		DateTime? End { get; }
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

			if (extends.Start == null)
				return new TimeSpan();

			DateTime end = (extends.End != null) ? (DateTime) extends.End : IcdEnvironment.GetLocalTime();

			return end - (DateTime) extends.Start;
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