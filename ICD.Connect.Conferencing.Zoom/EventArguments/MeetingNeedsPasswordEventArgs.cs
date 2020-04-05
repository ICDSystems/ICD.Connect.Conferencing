using System;

namespace ICD.Connect.Conferencing.Zoom.EventArguments
{
	public sealed class MeetingNeedsPasswordEventArgs : EventArgs
	{
		private readonly string m_MeetingNumber;
		private readonly string m_MeetingNumberFormatted;
		private readonly bool m_NeedsPassword;
		private readonly bool m_WrongAndRetry;

		public string MeetingNumber { get { return m_MeetingNumber; } }

		public string MeetingNumberFormatted { get { return m_MeetingNumberFormatted; } }

		public bool NeedsPassword { get { return m_NeedsPassword; } }

		public bool WrongAndRetry { get { return m_WrongAndRetry; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="meetingNumber"></param>
		/// <param name="meetingNumberFormatted"></param>
		/// <param name="needsPassword"></param>
		/// <param name="wrongAndRetry"></param>
		public MeetingNeedsPasswordEventArgs(string meetingNumber, string meetingNumberFormatted,
		                                     bool needsPassword, bool wrongAndRetry)
		{
			m_MeetingNumber = meetingNumber;
			m_MeetingNumberFormatted = meetingNumberFormatted;
			m_NeedsPassword = needsPassword;
			m_WrongAndRetry = wrongAndRetry;
		}
	}
}
