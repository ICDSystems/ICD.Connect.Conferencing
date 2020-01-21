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

		public void PanTilt(eCameraPanTiltAction action)
		{
			m_CameraRepeater.PanTilt(action);
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
			yield return new ConsoleCommand("PTUp", "Moves the camera up", () => PanTilt(eCameraPanTiltAction.Up));
			yield return new ConsoleCommand("PTDown", "Moves the camera down", () => PanTilt(eCameraPanTiltAction.Down));
			yield return new ConsoleCommand("PTLeft", "Moves the camera left", () => PanTilt(eCameraPanTiltAction.Left));
			yield return new ConsoleCommand("PTRight", "Moves the camera right", () => PanTilt(eCameraPanTiltAction.Right));
			yield return new ConsoleCommand("StopPT", "Stops the current Pan Tilt movement", () => StopPanTilt());
		}

		#endregion
	}
}
