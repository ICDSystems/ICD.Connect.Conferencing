using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Points;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public interface IConferencePoint : IPoint<IConferenceDeviceControl>
	{
		/// <summary>
		/// The type of call to use the conference control for.
		/// </summary>
		eCallType Type { get; set; }

		/// <summary>
		/// Determines if the privacy mute control will be driven by the control system, and/or drive the control system.
		/// </summary>
		ePrivacyMuteFeedback PrivacyMuteMask { get; set; }
	}
}