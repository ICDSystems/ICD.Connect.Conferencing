using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.EventArguments
{
	public enum eConferenceRecordingStatus
	{
		Unknown = 0,
		Stopped = 1,
		Recording = 2,
		Paused = 3
	}

	public class ConferenceRecordingStatusEventArgs : GenericEventArgs<eConferenceRecordingStatus>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ConferenceRecordingStatusEventArgs(eConferenceRecordingStatus data)
			: base(data)
		{
		}
	}
}