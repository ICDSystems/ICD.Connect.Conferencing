using System.Collections.Generic;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public abstract class AbstractWebConference<TParticipant> : AbstractConference<TParticipant>, IWebConference
		where TParticipant : class, IWebParticipant
	{
		/// <summary>
		/// Leaves the conference, keeping the conference in tact for other participants.
		/// </summary>
		public abstract void LeaveConference();

		/// <summary>
		/// Ends the conference for all participants.
		/// </summary>
		public abstract void EndConference();

		/// <summary>
		/// Gets the sources in this conference.
		/// </summary>
		/// <returns></returns>
		public new abstract IEnumerable<IWebParticipant> GetParticipants();
	}
}
