using System.Linq;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public interface IWebConference : IConference<IWebParticipant>
	{
		/// <summary>
		/// Leaves the conference, keeping the conference in tact for other participants.
		/// </summary>
		void LeaveConference();

		/// <summary>
		/// Ends the conference for all participants.
		/// </summary>
		void EndConference();
	}

	public static class WebConferenceExtensions
	{
		public static void MuteAll(this IWebConference extends)
		{
			foreach (IWebParticipant participant in extends.GetParticipants().Reverse())
				participant.Mute(true);
		}

		public static void UnmuteAll(this IWebConference extends)
		{
			foreach (IWebParticipant participant in extends.GetParticipants().Reverse())
				participant.Mute(false);
		}

		public static void KickAll(this IWebConference extends)
		{
			foreach(IWebParticipant participant in extends.GetParticipants().Reverse())
				participant.Kick();
		}
	}
}