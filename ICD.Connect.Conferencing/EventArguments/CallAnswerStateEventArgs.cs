using ICD.Connect.Conferencing.Participants;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class CallAnswerStateEventArgs : GenericEventArgs<eCallAnswerState>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public CallAnswerStateEventArgs(eCallAnswerState data) : base(data)
		{
		}
	}
}
