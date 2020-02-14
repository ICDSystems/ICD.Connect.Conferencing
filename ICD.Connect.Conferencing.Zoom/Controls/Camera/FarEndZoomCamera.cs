using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Cameras;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.Zoom.Components.Camera;

namespace ICD.Connect.Conferencing.Zoom.Controls.Camera
{
	public sealed class FarEndZoomCamera : IRemoteCamera
	{
		private readonly ZoomRoomCameraRepeater m_CameraRepeater;

		public FarEndZoomCamera(CameraComponent cameraComponent, string user)
		{
			m_CameraRepeater = new ZoomRoomCameraRepeater(cameraComponent, user);
		}

		public void Pan(eCameraPanAction action)
		{
			m_CameraRepeater.Pan(action);
		}

		public void Tilt(eCameraTiltAction action)
		{
			m_CameraRepeater.Tilt(action);
		}

		public void StopPanTilt()
		{
			m_CameraRepeater.StopPanTilt();
		}

		public void Zoom(eCameraZoomAction action)
		{
			m_CameraRepeater.Zoom(action);
		}

		public void StopZoom()
		{
			m_CameraRepeater.StopZoom();
		}

		public void RequestFarEndControl()
		{
			m_CameraRepeater.RequestControl();
		}

		public void GiveUpFarEndControl()
		{
			m_CameraRepeater.GiveUpControl();
		}

		#region Console

		public string ConsoleName { get { return "Camera"; } }

		public string ConsoleHelp { get { return "Camera Controls for the Zoom Participant."; } }

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield break;
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("UserId", m_CameraRepeater.UserId);
			addRow("Controls Available", m_CameraRepeater.HaveControl);
		}

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("RequestControl", "Requests control of the far end camera", () => RequestFarEndControl());
			yield return new ConsoleCommand("GiveUpControl", "Gives up control of the far end camera", () => GiveUpFarEndControl());
			yield return new ConsoleCommand("ZoomIn", "Zooms the camera in.", () => Zoom(eCameraZoomAction.ZoomIn));
			yield return new ConsoleCommand("ZoomOut", "Zooms the camera out", () => Zoom(eCameraZoomAction.ZoomOut));
			yield return new ConsoleCommand("StopZoom", "Stops the current zoom action", () => StopZoom());
			yield return new ConsoleCommand("TiltUp", "Moves the camera up", () => Tilt(eCameraTiltAction.Up));
			yield return new ConsoleCommand("TiltDown", "Moves the camera down", () => Tilt(eCameraTiltAction.Down));
			yield return new ConsoleCommand("PanLeft", "Moves the camera left", () => Pan(eCameraPanAction.Left));
			yield return new ConsoleCommand("PanRight", "Moves the camera right", () => Pan(eCameraPanAction.Right));
			yield return new ConsoleCommand("StopPT", "Stops the current Pan Tilt movement", () => StopPanTilt());
		}

		#endregion
	}
}
