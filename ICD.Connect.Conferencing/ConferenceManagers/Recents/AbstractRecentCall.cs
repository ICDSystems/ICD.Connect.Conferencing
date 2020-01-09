using System;

namespace ICD.Connect.Conferencing.ConferenceManagers.Recents
{
	public abstract class AbstractRecentCall : IRecentCall
	{
		public abstract string Name { get; }
		public abstract string Number { get; }
		public abstract DateTime Time { get; }
	}
}
