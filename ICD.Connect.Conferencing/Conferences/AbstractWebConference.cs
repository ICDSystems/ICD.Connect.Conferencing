using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public abstract class AbstractWebConference : AbstractConference<IWebParticipant>, IWebConference
	{
		/// <summary>
		/// Leaves the conference, keeping the conference in tact for other participants.
		/// </summary>
		public abstract void LeaveConference();

		/// <summary>
		/// Ends the conference for all participants.
		/// </summary>
		public abstract void EndConference();
	}
}
