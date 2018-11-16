using ICD.Connect.Conferencing.Conferences;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public interface IWebConferenceDeviceControl : IConferenceDeviceControl<IWebConference>
	{
		void SetCameraEnabled(bool enabled);

		bool CameraEnabled { get; }
	}
}