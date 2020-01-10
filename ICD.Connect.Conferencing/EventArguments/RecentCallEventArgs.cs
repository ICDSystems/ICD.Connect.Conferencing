using System;
using ICD.Connect.Conferencing.ConferenceManagers.Recents;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class RecentCallEventArgs : EventArgs
	{
		private readonly IRecentCall m_RecentCall;
		private readonly bool m_Added;

		public IRecentCall RecentCall { get { return m_RecentCall; } }

		public bool Added { get { return m_Added; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="recentCall"></param>
		/// <param name="added"></param>
		public RecentCallEventArgs(IRecentCall recentCall, bool added)
		{
			m_RecentCall = recentCall;
			m_Added = added;
		}
	}
}
