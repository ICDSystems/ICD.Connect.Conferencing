using ICD.Common.EventArguments;

namespace ICD.Connect.Conferencing.Conferences
{
	/// <summary>
	/// Conference States
	/// </summary>
	public enum eConferenceStatus
	{
		Undefined,
		Connecting,
		Connected,
		Disconnected,
// ReSharper disable InconsistentNaming
		OnHold
// ReSharper restore InconsistentNaming
	}

	public sealed class ConferenceStatusEventArgs : GenericEventArgs<eConferenceStatus>
	{
		/// <summary>
		/// Primary Constructor
		/// </summary>
		/// <param name="state"></param>
		public ConferenceStatusEventArgs(eConferenceStatus state) : base(state)
		{
		}
	}
}
