namespace ICD.Connect.Conferencing.Cameras
{
	public enum eCameraAction
	{
		Left,
		Right,
		Up,
		Down,

		ZoomIn,
		ZoomOut
	}

	public interface ICamera
	{
		/// <summary>
		/// Starts the camera moving.
		/// </summary>
		/// <param name="action"></param>
		void Move(eCameraAction action);

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		void Stop();
	}
}
