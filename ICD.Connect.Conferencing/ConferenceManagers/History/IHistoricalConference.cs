using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Conferences;

namespace ICD.Connect.Conferencing.ConferenceManagers.History
{
	public interface IHistoricalConference
	{
		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		event EventHandler<StringEventArgs> OnNameChanged;

		string Name { get; }

		DateTime? StartTime { get; }
		DateTime? EndTime { get; }

		/// <summary>
		/// Gets the status of the conference
		/// </summary>
		eConferenceStatus Status { get; }

		IEnumerable<IHistoricalParticipant> GetParticipants();

		/// <summary>
		/// Detach HistoricalConference from the underlying conference/incoming call
		/// This is called when the conference gets removed, to unsubscribe
		/// and remove references to the conference
		/// </summary>
		void Detach();
	}
}
