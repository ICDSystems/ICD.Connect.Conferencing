using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.EventArguments
{
	public enum eConferenceSourceType
	{
		Unknown,
		Audio,
		Video
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
