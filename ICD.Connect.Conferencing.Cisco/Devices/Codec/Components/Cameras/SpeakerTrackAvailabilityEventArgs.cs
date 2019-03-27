using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
{
	public enum eSpeakerTrackAvailability
	{
		Off,
		Unavailable,
		Available
	}

	public sealed class SpeakerTrackAvailabilityEventArgs : GenericEventArgs<eSpeakerTrackAvailability>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SpeakerTrackAvailabilityEventArgs(eSpeakerTrackAvailability data)
			: base(data)
		{
		}
	}
}
