using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.ConferenceSources;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceSourceEventArgs : GenericEventArgs<IConferenceSource>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="source"></param>
		public ConferenceSourceEventArgs(IConferenceSource source) : base(source)
		{
		}
	}
}
