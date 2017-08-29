using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.ConferenceSources;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceSourceAnswerStateEventArgs : GenericEventArgs<eConferenceSourceAnswerState>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="state"></param>
		public ConferenceSourceAnswerStateEventArgs(eConferenceSourceAnswerState state) : base(state)
		{
		}
	}
}
