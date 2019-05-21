using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
{
	public enum ePresenterTrackAvailability
	{
		Off,
		Unavailable,
		Available
	}

	public sealed class PresenterTrackAvailabilityEventArgs : GenericEventArgs<ePresenterTrackAvailability>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public PresenterTrackAvailabilityEventArgs(ePresenterTrackAvailability data)
			: base(data)
		{
		}
	}
}