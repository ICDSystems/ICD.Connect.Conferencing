using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
{
	public enum eSpeakerTrackAvailability
	{
		Unavailable,
		Off,
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
