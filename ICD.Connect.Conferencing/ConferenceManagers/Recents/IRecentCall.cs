using System;

namespace ICD.Connect.Conferencing.ConferenceManagers.Recents
{
	public interface IRecentCall
	{
		string Name { get; }
		string Number { get; }
		DateTime Time { get; }
	}
}
