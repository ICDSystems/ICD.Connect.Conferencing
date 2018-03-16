using ICD.Connect.Cameras;

namespace ICD.Connect.Conferencing.Cameras
{
	public interface IRemoteCamera
	{
		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		void PanTilt(eCameraPanTiltAction action);

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		void StopPanTilt();
	}
}
