using System;
using System.Collections.Generic;
using ICD.Common.Utils.Collections;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Components.Camera;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomCameraControl : AbstractCameraDeviceControl<ZoomRoom>, IPanTiltControl, IZoomControl
	{
		private readonly CameraComponent m_CameraComponent;
		private readonly CallComponent m_CallComponent;

		private Queue<BiDictionary<eCameraControlState, eCameraControlAction>> m_MostRecentCommand;

		public ZoomRoomCameraControl(ZoomRoom parent, int id) 
			: base(parent, id)
		{
			m_MostRecentCommand = new Queue<BiDictionary<eCameraControlState, eCameraControlAction>>();

			m_CameraComponent = parent.Components.GetComponent<CameraComponent>();
			m_CallComponent = parent.Components.GetComponent<CallComponent>();

			Subscribe(m_CameraComponent);
			Subscribe(m_CallComponent);
		}

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			m_MostRecentCommand = null;

			Unsubscribe(m_CameraComponent);
			Unsubscribe(m_CallComponent);
		}

		#region Pan Tilt

		void IPanTiltControl.Stop()
		{
			PanTilt(eCameraPanTiltAction.Stop);
		}

		public void PanLeft()
		{
			PanTilt(eCameraPanTiltAction.Left);
		}

		public void PanRight()
		{
			PanTilt(eCameraPanTiltAction.Right);
		}

		public void TiltUp()
		{
			PanTilt(eCameraPanTiltAction.Up);
		}

		public void TiltDown()
		{
			PanTilt(eCameraPanTiltAction.Down);
		}

		public void PanTilt(eCameraPanTiltAction action)
		{
			switch (action)
			{
				case eCameraPanTiltAction.Left:
					break;
				case eCameraPanTiltAction.Right:
					break;
				case eCameraPanTiltAction.Up:
					break;
				case eCameraPanTiltAction.Down:
					break;
				case eCameraPanTiltAction.Stop:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(action), action, null);
			}
		}

		#endregion

		#region Zoom

		void IZoomControl.Stop()
		{
			Zoom(eCameraZoomAction.Stop);
		}

		public void ZoomIn()
		{
			Zoom(eCameraZoomAction.ZoomIn);
		}

		public void ZoomOut()
		{
			Zoom(eCameraZoomAction.ZoomOut);
		}

		public void Zoom(eCameraZoomAction action)
		{
			switch (action)
			{
				case eCameraZoomAction.ZoomIn:
					break;
				case eCameraZoomAction.ZoomOut:
					break;
				case eCameraZoomAction.Stop:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(action), action, null);
			}
		}

		#endregion

		private void UpdateLastCommand()
		{

		}

		#region Camera Component Callbacks

		private void Subscribe(CameraComponent cameraComponent)
		{
			throw new System.NotImplementedException();
		}

		private void Unsubscribe(CameraComponent cameraComponent)
		{
			throw new System.NotImplementedException();
		}

		#endregion

		#region Call Component Callbacks

		private void Subscribe(CallComponent cameraComponent)
		{
			throw new NotImplementedException();
		}

		private void Unsubscribe(CallComponent cameraComponent)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
