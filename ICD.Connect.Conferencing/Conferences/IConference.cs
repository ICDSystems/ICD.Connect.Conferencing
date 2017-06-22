using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.ConferenceSources;

namespace ICD.Connect.Conferencing.Conferences
{
	/// <summary>
	/// A IConference is a collection of IConferenceSources.
	/// </summary>
	public interface IConference
	{
		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when a source is added or removed to the conference.
		/// </summary>
		event EventHandler OnSourcesChanged;

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

		/// <summary>
		/// Gets the number of sources in the conference.
		/// </summary>
		int SourcesCount { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Gets the sources in this conference.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IConferenceSource> GetSources();

		/// <summary>
		/// Adds the source to the conference.
		/// </summary>
		/// <param name="source"></param>
		/// <returns>False if the source is already in the conference.</returns>
		[PublicAPI]
		bool AddSource(IConferenceSource source);

		/// <summary>
		/// Removes the source from the conference.
		/// </summary>
		/// <param name="source"></param>
		/// <returns>False if the source is not in the conference.</returns>
		[PublicAPI]
		bool RemoveSource(IConferenceSource source);

		#endregion
	}

	/// <summary>
	/// Extension methods for IConferences.
	/// </summary>
	public static class ConferenceExtensions
	{
		/// <summary>
		/// Holds all sources.
		/// </summary>
		/// <param name="extends"></param>
		public static void Hold(this IConference extends)
		{
			foreach (IConferenceSource source in extends.GetSources().Reverse())
				source.Hold();
		}

		/// <summary>
		/// Resumes all sources.
		/// </summary>
		/// <param name="extends"></param>
		public static void Resume(this IConference extends)
		{
			foreach (IConferenceSource source in extends.GetSources().Reverse())
				source.Resume();
		}

		/// <summary>
		/// Disconnects all sources.
		/// </summary>
		/// <param name="extends"></param>
		public static void Hangup(this IConference extends)
		{
			foreach (IConferenceSource source in extends.GetSources().Reverse())
				source.Hangup();
		}

		/// <summary>
		/// Gets the duration of the call in milliseconds.
		/// </summary>
		/// <param name="extends"></param>
		public static TimeSpan GetDuration(this IConference extends)
		{
			if (extends.Start == null)
				return new TimeSpan();

			DateTime end = (extends.End != null) ? (DateTime)extends.End : IcdEnvironment.GetLocalTime();

			return end - (DateTime)extends.Start;
		}

		/// <summary>
		/// Returns true if the conference contains the given source.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="source"></param>
		/// <returns></returns>
		public static bool ContainsSource(this IConference extends, IConferenceSource source)
		{
			return extends.GetSources().Contains(source);
		}

		/// <summary>
		/// Returns an array of online sources.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static IConferenceSource[] GetOnlineSources(this IConference extends)
		{
			return extends.GetSources().Where(s => s.GetIsOnline()).ToArray();
		}
	}
}
