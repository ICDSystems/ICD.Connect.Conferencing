namespace ICD.Connect.Conferencing.Participants
{
	/// <summary>
	/// A web participant represents a conference participant using web-based
	/// protocols, like Zoom, Skype, etc.
	/// </summary>
	public interface IWebParticipant : IParticipant
	{
		/// <summary>
		/// Kick the participant from the conference.
		/// </summary>
		/// <returns></returns>
		void Kick();

		/// <summary>
		/// Mute the participant in the conference.
		/// </summary>
		/// <returns></returns>
		void Mute(bool mute);

		bool IsMuted { get; }
	}
}