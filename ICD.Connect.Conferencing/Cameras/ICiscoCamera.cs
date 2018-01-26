using ICD.Connect.Cameras;

namespace ICD.Connect.Conferencing.Cameras
{
	

	public interface ICiscoCamera
	{
		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		void Move(eCameraPanTiltAction action);

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		void Stop();
	}
}
