using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Participants.Enums;

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
