using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
{
	public enum eSpeakerTrackStatus
	{
		Inactive,
		Active
	}

	public sealed class SpeakerTrackStatusEventArgs : GenericEventArgs<eSpeakerTrackStatus>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SpeakerTrackStatusEventArgs(eSpeakerTrackStatus data)
			: base(data)
		{
		}
	}
}