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
		public static IEnumerable<IParticipant> GetOnlineParticipants(this IConference extends)
		{
			return extends.GetParticipants().Where(p => p.GetIsOnline());
		}

		public static bool IsOnline(this IConference extends)
		{
			return extends.Status == eConferenceStatus.Connected || extends.Status == eConferenceStatus.OnHold;
		}

		public static bool IsActive(this IConference extends)
		{
			return extends.Status == eConferenceStatus.Connected;
		}
	}
}