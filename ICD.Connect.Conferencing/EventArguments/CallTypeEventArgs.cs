using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.EventArguments
{
	[Flags]
	public enum eCallType
	{
		Unknown = 0,
		Audio = 1,
		Video = 2
	}

	public sealed class CallTypeEventArgs : GenericEventArgs<eCallType>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public CallTypeEventArgs(eCallType data)
			: base(data)
		{
		}
	}
}
