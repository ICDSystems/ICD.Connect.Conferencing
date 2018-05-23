using System;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.EventArguments
{
	[Flags]
	public enum eConferenceSourceType
	{
		Unknown = 0,
		Audio = 1,
		Video = 2
	}

	public sealed class ConferenceSourceTypeEventArgs : GenericEventArgs<eConferenceSourceType>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ConferenceSourceTypeEventArgs(eConferenceSourceType data)
			: base(data)
		{
		}
	}
}
