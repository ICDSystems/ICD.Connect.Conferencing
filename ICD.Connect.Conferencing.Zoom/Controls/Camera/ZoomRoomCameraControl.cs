using System.Collections.Generic;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Conferencing.Zoom.Components.Camera;

namespace ICD.Connect.Conferencing.Zoom.Controls.Camera
{
	public sealed class ZoomRoomCameraControl : AbstractCameraDeviceControl<ZoomRoom>, IPanTiltControl, IZoomControl
	{
		private readonly ZoomRoomCameraRepeater m_CameraRepeater;

		public ZoomRoomCameraControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			CameraComponent cameraComponent = parent.Components.GetComponent<CameraComponent>();
			m_CameraRepeater = new ZoomRoomCameraRepeater(cameraComponent, "0");
		}

		#region Pan Tilt

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		void IPanTiltControl.Stop()
		{
			m_CameraRepeater.StopPanTilt();
		}

		/// <summary>
		/// Begin panning the camera to the left.
		/// </summary>
		public void PanLeft()
		{
			PanTilt(eCameraPanTiltAction.Left);
		}

		/// <summary>
		/// Begin panning the camera to the right.
		/// </summary>
		public void PanRight()
		{
			PanTilt(eCameraPanTiltAction.Right);
		}

		/// <summary>
		/// Begin tilting the camera up.
		/// </summary>
		public void TiltUp()
		{
			PanTilt(eCameraPanTiltAction.Up);
		}

		/// <summary>
		/// Begin tilting the camera down.
		/// </summary>
		public void TiltDown()
		{
			PanTilt(eCameraPanTiltAction.Down);
		}

		/// <summary>
		/// Performs the given pan/tilt action.
		/// </summary>
		/// <param name="action"></param>
		public void PanTilt(eCameraPanTiltAction action)
		{
			m_CameraRepeater.PanTilt(action);
		}

		#endregion

		#region Zoom

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		void IZoomControl.Stop()
		{
			m_CameraRepeater.StopZoom();
		}

		/// <summary>
		/// Begin zooming the camera in.
		/// </summary>
		public void ZoomIn()
		{
			Zoom(eCameraZoomAction.ZoomIn);
		}

		/// <summary>
		/// Begin zooming the camera out.
		/// </summary>
		public void ZoomOut()
		{
			Zoom(eCameraZoomAction.ZoomOut);
		}

		/// <summary>
		/// Performs the given zoom action.
		/// </summary>
		public void Zoom(eCameraZoomAction action)
		{
			m_CameraRepeater.Zoom(action);
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in PanTiltControlConsole.GetConsoleNodes(this))
				yield return node;

			foreach (IConsoleNodeBase node in ZoomControlConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			PanTiltControlConsole.BuildConsoleStatus(this, addRow);
			ZoomControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in PanTiltControlConsole.GetConsoleCommands(this))
				yield return command;

			foreach (IConsoleCommand command in ZoomControlConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
