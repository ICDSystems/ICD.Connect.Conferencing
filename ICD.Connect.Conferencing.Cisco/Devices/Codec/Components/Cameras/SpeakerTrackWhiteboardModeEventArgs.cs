using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
{
	public enum eSpeakerTrackWhiteboardMode
	{
		Off,
		On
	}

	public sealed class SpeakerTrackWhiteboardModeEventArgs : GenericEventArgs<eSpeakerTrackWhiteboardMode>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SpeakerTrackWhiteboardModeEventArgs(eSpeakerTrackWhiteboardMode data)
			: base(data)
		{
		}
	}
}
