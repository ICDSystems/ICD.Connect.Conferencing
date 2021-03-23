using System;
using System.Collections.Generic;
using ICD.Connect.Cameras;
using ICD.Connect.Cameras.Controls;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Camera;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Controls.Camera
{
	public sealed class ZoomRoomCameraControl : AbstractCameraDeviceControl<ZoomRoom>
	{
		public override event EventHandler<CameraControlPresetsChangedApiEventArgs> OnPresetsChanged;
		public override event EventHandler<CameraControlFeaturesChangedApiEventArgs> OnSupportedCameraFeaturesChanged;
		public override event EventHandler<CameraControlMuteChangedApiEventArgs> OnCameraMuteStateChanged;

		private readonly ZoomRoomCameraRepeater m_CameraRepeater;

		public override eCameraFeatures SupportedCameraFeatures { get { return eCameraFeatures.PanTiltZoom; } }

		/// <summary>
		/// Gets the maximum number of presets this camera can support.
		/// </summary>
		public override int MaxPresets { get { return 0; } }

		/// <summary>
		/// Gets whether the camera is currently muted
		/// </summary>
		public override bool IsCameraMuted { get { return false; } }

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
		public override void PanStop()
		{
			m_CameraRepeater.Pan(eCameraPanAction.Stop);
		}

		/// <summary>
		/// Begin panning the camera to the left.
		/// </summary>
		public override void PanLeft()
		{
			m_CameraRepeater.Pan(eCameraPanAction.Left);
		}

		/// <summary>
		/// Begin panning the camera to the right.
		/// </summary>
		public override void PanRight()
		{
			m_CameraRepeater.Pan(eCameraPanAction.Right);
		}

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		public override void TiltStop()
		{
			m_CameraRepeater.Tilt(eCameraTiltAction.Stop);
		}

		/// <summary>
		/// Begin tilting the camera up.
		/// </summary>
		public override void TiltUp()
		{
			m_CameraRepeater.Tilt(eCameraTiltAction.Up);
		}

		/// <summary>
		/// Begin tilting the camera down.
		/// </summary>
		public override void TiltDown()
		{
			m_CameraRepeater.Tilt(eCameraTiltAction.Down);
		}

		#endregion

		#region Zoom

		/// <summary>
		/// Begin zooming the camera in.
		/// </summary>
		public override void ZoomIn()
		{
			m_CameraRepeater.Zoom(eCameraZoomAction.ZoomIn);
		}

		/// <summary>
		/// Begin zooming the camera out.
		/// </summary>
		public override void ZoomOut()
		{
			m_CameraRepeater.Zoom(eCameraZoomAction.ZoomOut);
		}

		/// <summary>
		/// Stops the camera from moving.
		/// </summary>
		public override void ZoomStop()
		{
			m_CameraRepeater.Zoom(eCameraZoomAction.Stop);
		}

		#endregion

		#region Presets

		/// <summary>
		/// Gets the stored camera presets.
		/// </summary>
		public override IEnumerable<CameraPreset> GetPresets()
		{
			yield break;
		}

		/// <summary>
		/// Tells the camera to change its position to the given preset.
		/// </summary>
		/// <param name="presetId">The id of the preset to position to.</param>
		public override void ActivatePreset(int presetId)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Stores the cameras current position in the given preset index.
		/// </summary>
		/// <param name="presetId">The index to store the preset at.</param>
		public override void StorePreset(int presetId)
		{
			throw new NotSupportedException();
		}

		#endregion

		/// <summary>
		/// Sets if the camera mute state should be active
		/// </summary>
		/// <param name="enable"></param>
		public override void MuteCamera(bool enable)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Resets camera to its predefined home position
		/// </summary>
		public override void ActivateHome()
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Stores the current position as the home position.
		/// </summary>
		public override void StoreHome()
		{
			throw new NotSupportedException();
		}
	}
}
