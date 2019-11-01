using System;
using System.Collections.Generic;
using System.Linq;
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

		#region Properties

		/// <summary>
		/// Current conference status.
		/// </summary>
		eConferenceStatus Status { get; }

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		DateTime? Start { get; }

		/// <summary>
		/// The time the call ended.
		/// </summary>
		DateTime? End { get; }

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
		public static IEnumerable<IParticipant> GetOnlineParticipants(this IConference extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return extends.GetParticipants().Where(p => p.GetIsOnline());
		}

		/// <summary>
		/// Returns true if this conference is currently online.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool IsOnline(this IConference extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			switch (extends.Status)
			{
				case eConferenceStatus.Undefined:
				case eConferenceStatus.Connecting:
				case eConferenceStatus.Disconnecting: // TODO - Disconnecting hasn't disconnected yet, online?
				case eConferenceStatus.Disconnected:
					return false;

				case eConferenceStatus.Connected:
				case eConferenceStatus.OnHold:
					return true;
				
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Returns true if this conference is not disconnected.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool IsActive(this IConference extends)
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
		public static TimeSpan GetDuration(this IConference extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (!extends.Start.HasValue)
				return TimeSpan.Zero;

			DateTime start = extends.Start.Value;
			DateTime end = extends.End ?? DateTime.Now;

			return end - start;
		}
	}
}
