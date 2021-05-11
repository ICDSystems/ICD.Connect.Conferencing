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
	[Flags]
	public enum eConferenceFeatures
	{
		None = 0,

		GetStatus = 1,

		GetStartTime = 2,

		GetEndTime = 4,

		GetCallType = 8,

		GetParticipants = 16,

		LeaveConference = 32,

		EndConference = 64
	}

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

		/// <summary>
		/// Raised when the supported conference features changes.
		/// </summary>
		event EventHandler<GenericEventArgs<eConferenceFeatures>> OnSupportedConferenceFeaturesChanged;

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

		/// <summary>
		/// Gets the supported conference features.
		/// </summary>
		eConferenceFeatures SupportedConferenceFeatures { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets the sources in this conference.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IParticipant> GetParticipants();

		/// <summary>
		/// Leaves the conference, keeping the conference in tact for other participants.
		/// </summary>
		void LeaveConference();

		/// <summary>
		/// Ends the conference for all participants.
		/// </summary>
		void EndConference();

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

		public static void MuteAll(this IConference extends)
		{
			foreach (IParticipant participant in extends.GetParticipants().Reverse())
				participant.Mute(true);
		}

		public static void UnmuteAll(this IConference extends)
		{
			foreach (IParticipant participant in extends.GetParticipants().Reverse())
				participant.Mute(false);
		}

		public static void KickAll(this IConference extends)
		{
			foreach (IParticipant participant in extends.GetParticipants().Reverse())
				participant.Kick();
		}

		/// <summary>
		/// Holds all sources.
		/// </summary>
		/// <param name="extends"></param>
		public static void Hold(this IConference extends)
		{
			foreach (IParticipant participant in extends.GetParticipants().Reverse())
				participant.Hold();
		}

		/// <summary>
		/// Resumes all sources.
		/// </summary>
		/// <param name="extends"></param>
		public static void Resume(this IConference extends)
		{
			foreach (IParticipant participant in extends.GetParticipants().Reverse())
				participant.Resume();
		}

		/// <summary>
		/// Disconnects all sources.
		/// </summary>
		/// <param name="extends"></param>
		public static void Hangup(this IConference extends)
		{
			foreach (IParticipant participant in extends.GetParticipants().Reverse())
				participant.Hangup();
		}

		/// <summary>
		/// Returns true if the conference contains the given participant.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="source"></param>
		/// <returns></returns>
		public static bool ContainsSource(this IConference extends, IParticipant source)
		{
			return extends.GetParticipants().Contains(source);
		}

		/// <summary>
		/// Returns an array of online sources.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static IParticipant[] GetOnlineSources(this IConference extends)
		{
			return extends.GetParticipants().Where(s => s.GetIsOnline()).ToArray();
		}
	}
}
