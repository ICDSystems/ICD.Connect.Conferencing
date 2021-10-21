using ICD.Connect.API.Nodes;
using ICD.Connect.Cameras;

namespace ICD.Connect.Conferencing.Cameras
{
	public interface IRemoteCamera : IConsoleNodeBase
	{
		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		void Pan(eCameraPanAction action);

		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		void Tilt(eCameraTiltAction action);

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		void StopPanTilt();

		/// <summary>
		/// Zooms the camera
		/// </summary>
		/// <param name="action"></param>
		void Zoom(eCameraZoomAction action);

		/// <summary>
		/// Stops the camera from zooming
		/// </summary>
		void StopZoom();
	}
}
