using System;
using System.Linq;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	/// <summary>
	/// A ITraditionalConference is a conference with one or more ITraditionalParticipants.
	/// </summary>
	public interface ITraditionalConference : IConference<ITraditionalParticipant>
	{
	}

	/// <summary>
	/// Extension methods for IConferences.
	/// </summary>
	public static class TraditionalConferenceExtensions
	{
		/// <summary>
		/// Holds all sources.
		/// </summary>
		/// <param name="extends"></param>
		public static void Hold(this ITraditionalConference extends)
		{
			foreach (ITraditionalParticipant participant in extends.GetParticipants().Reverse())
				participant.Hold();
		}

		/// <summary>
		/// Resumes all sources.
		/// </summary>
		/// <param name="extends"></param>
		public static void Resume(this ITraditionalConference extends)
		{
			foreach (ITraditionalParticipant participant in extends.GetParticipants().Reverse())
				participant.Resume();
		}

		/// <summary>
		/// Disconnects all sources.
		/// </summary>
		/// <param name="extends"></param>
		public static void Hangup(this ITraditionalConference extends)
		{
			foreach (ITraditionalParticipant participant in extends.GetParticipants().Reverse())
				participant.Hangup();
		}

		/// <summary>
		/// Gets the duration of the call in milliseconds.
		/// </summary>
		/// <param name="extends"></param>
		public static TimeSpan GetDuration(this ITraditionalConference extends)
		{
			if (extends.Start == null)
				return new TimeSpan();

			DateTime end = (extends.End != null) ? (DateTime)extends.End : IcdEnvironment.GetUtcTime();

			return end - (DateTime)extends.Start;
		}

		/// <summary>
		/// Returns true if the conference contains the given participant.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="source"></param>
		/// <returns></returns>
		public static bool ContainsSource(this ITraditionalConference extends, ITraditionalParticipant source)
		{
			return extends.GetParticipants().Contains(source);
		}

		/// <summary>
		/// Returns an array of online sources.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static ITraditionalParticipant[] GetOnlineSources(this ITraditionalConference extends)
		{
			return extends.GetParticipants().Where(s => s.GetIsOnline()).ToArray();
		}
	}
}
