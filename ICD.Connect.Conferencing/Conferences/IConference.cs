using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public interface IConference : IConsoleNode
	{
		/// <summary>
		/// Raised when a participant is added to the conference.
		/// </summary>
		event EventHandler<ParticipantEventArgs> OnParticipantAdded;

		/// <summary>
		/// Raised when a participant is removed from the conference.
		/// </summary>
		event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the start time changes
		/// </summary>
		event EventHandler<DateTimeNullableEventArgs> OnStartTimeChanged;

		/// <summary>
		/// Raised when the end time changes
		/// </summary>
		event EventHandler<DateTimeNullableEventArgs> OnEndTimeChanged;

		/// <summary>
		/// Raised when the conference's call type changes.
		/// </summary>
		event EventHandler<GenericEventArgs<eCallType>> OnCallTypeChanged;

		#region Properties

		/// <summary>
		/// Current conference status.
		/// </summary>
		eConferenceStatus Status { get; }

		/// <summary>
		/// The time the conference started.
		/// </summary>
		DateTime? StartTime { get; }

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		DateTime? EndTime { get; }

		/// <summary>
		/// Gets the type of call.
		/// </summary>
		eCallType CallType { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets the sources in this conference.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IParticipant> GetParticipants();

		#endregion
	}

	public interface IConference<T> : IConference where T : IParticipant
	{
		#region Methods

		/// <summary>
		/// Gets the sources in this conference.
		/// </summary>
		/// <returns></returns>
		new IEnumerable<T> GetParticipants();

		#endregion
	}

	public static class ConferenceExtensions
	{
		/// <summary>
		/// Gets the participants in this conference who are currently online.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static IEnumerable<IParticipant> GetOnlineParticipants([NotNull] this IConference extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return extends.GetParticipants().Where(p => p.GetIsOnline());
		}

		/// <summary>
		/// Returns true if this conference is not disconnected.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool IsActive([NotNull] this IConference extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			switch (extends.Status)
			{
				case eConferenceStatus.Undefined:
				case eConferenceStatus.Disconnected:
					return false;
				
				case eConferenceStatus.Connecting:
				case eConferenceStatus.Connected:
				case eConferenceStatus.Disconnecting:
				case eConferenceStatus.OnHold:
					return true;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Returns the current duration of the conference.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static TimeSpan GetDuration([NotNull] this IConference extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (!extends.StartTime.HasValue)
				return TimeSpan.Zero;

			DateTime start = extends.StartTime.Value;
			DateTime end = extends.EndTime ?? IcdEnvironment.GetUtcTime();

			return end - start;
		}
	}
}
