using System;
using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants.Enums;

namespace ICD.Connect.Conferencing.ConferenceManagers.History
{
	public interface IHistoricalConference
	{
		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the conference name changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the conference number changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNumberChanged;

		/// <summary>
		/// Raised when the conference direction changes.
		/// </summary>
		event EventHandler<GenericEventArgs<eCallDirection>> OnDirectionChanged;

		/// <summary>
		/// Raised when the conference answer state changes.
		/// </summary>
		event EventHandler<GenericEventArgs<eCallAnswerState>> OnAnswerStateChanged;

		/// <summary>
		/// Name of the conference
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Number of the conferenc for redial, etc
		/// </summary>
		string Number { get; }

		/// <summary>
		/// Direction
		/// </summary>
		eCallDirection Direction { get; }

		/// <summary>
		/// Answer State
		/// </summary>
		eCallAnswerState AnswerState { get; }

		/// <summary>
		/// Call Type
		/// </summary>
		eCallType CallType { get; }

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

		/// <summary>
		/// Gets the underlying historical conference type.
		/// </summary>
		/// <returns></returns>
		Type GetConferenceType();
	}
}
