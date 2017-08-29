using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Conferences;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceEventArgs : GenericEventArgs<IConference>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="conference"></param>
		public ConferenceEventArgs(IConference conference) : base(conference)
		{
		}
	}
}
