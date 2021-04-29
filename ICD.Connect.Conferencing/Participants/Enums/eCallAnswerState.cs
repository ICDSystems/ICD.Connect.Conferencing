namespace ICD.Connect.Conferencing.Participants.Enums
{
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
}