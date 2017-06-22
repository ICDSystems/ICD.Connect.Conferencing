using ICD.Common.EventArguments;

namespace ICD.Connect.Conferencing.EventArguments
{
	// TODO - Currently coupled to Cisco Codec output, need to remove unused items
	public enum eConferenceSourceStatus
	{
		Undefined,
		Idle,
		Dialing,
		Ringing,
		Connecting,
		Connected,
		Disconnecting,
// ReSharper disable InconsistentNaming
		OnHold,
// ReSharper restore InconsistentNaming
		EarlyMedia,
		Preserved,
		RemotePreserved,
		Disconnected
	}

	public sealed class ConferenceSourceStatusEventArgs : GenericEventArgs<eConferenceSourceStatus>
	{
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="status"></param>
		public ConferenceSourceStatusEventArgs(eConferenceSourceStatus status) : base(status)
		{
		}
	}
}
