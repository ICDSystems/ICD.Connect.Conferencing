using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.ConferenceManagers.History
{
	public sealed class HistoricalConferenceEventArgs : GenericEventArgs<IHistoricalConference>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="conference"></param>
		public HistoricalConferenceEventArgs(IHistoricalConference conference):base(conference)
		{
		}
	}
}
