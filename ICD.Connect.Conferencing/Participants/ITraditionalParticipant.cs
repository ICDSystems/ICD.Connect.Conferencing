using System;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Participants
{
	public enum eCallDirection
	{
		Undefined = 0,
		Incoming = 1,
		Outgoing = 2
	}

	/// <summary>
	/// Answer state
	/// </summary>
	public enum eCallAnswerState
	{
		/// <summary>
		/// No known state
		/// </summary>
		Unknown = 0,

		/// <summary>
		/// Incoming: No decision has been made
		/// Outgoing: Remote end has not answered call
		/// </summary>
		Unanswered = 1,

		/// <summary>
		/// Incoming: Call stopped without user action
		/// Outgoing: N/A
		/// </summary>
		Ignored = 2,

		/// <summary>
		/// Incoming: When a call is actively rejected by the user, or DND setting
		/// Outgoing: Call was rejected by far end - busy signal, DND, Error, etc
		/// </summary>
		Rejected = 3,

		/// <summary>
		/// Incoming: Automatically answered by the software
		/// Outgoing: N/A
		/// </summary>
		AutoAnswered = 4,

		/// <summary>
		/// Incoming: Actively answered by the user
		/// Outgoing: Call connected to far end
		/// </summary>
		Answered = 5
	}

	/// <summary>
	/// A traditional participant represents a conference participant using traditional
	/// conferencing protocols (SIP, H.323, PSTN/POTS)
	/// </summary>
	public interface ITraditionalParticipant : IParticipant
	{
		/// <summary>
		/// Raised when the source number changes.
		/// </summary>
		event EventHandler<StringEventArgs> OnNumberChanged;

		/// <summary>
		/// Raised when the participant is answered, dismissed or ignored.
		/// </summary>
		event EventHandler<CallAnswerStateEventArgs> OnAnswerStateChanged;

		#region Properties

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

		#endregion
	}

	public static class TraditionalParticipantExtensions
	{
		/// <summary>
		/// Allows sending data to dial-tone menus.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="data"></param>
		public static void SendDtmf(this ITraditionalParticipant extends, char data)
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
		public static DateTime GetStartOrDialTime(this ITraditionalParticipant extends)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			return extends.StartTime ?? extends.DialTime;
		}
	}
}
