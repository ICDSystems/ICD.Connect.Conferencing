using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class IncomingCallAnswerStateEventArgs : GenericEventArgs<eCallAnswerState>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="state"></param>
		public IncomingCallAnswerStateEventArgs(eCallAnswerState state) : base(state)
		{
		}
	}
}
