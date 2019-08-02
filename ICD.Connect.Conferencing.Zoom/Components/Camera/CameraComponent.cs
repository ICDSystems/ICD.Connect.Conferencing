using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Camera
{
	public class CameraComponent : AbstractZoomRoomComponent
	{
		public event EventHandler OnCamerasUpdated;
		public event EventHandler OnActiveCameraUpdated;

		private CameraInfo[] m_Cameras;
		private string m_SelectedUsbId;

		public CameraInfo ActiveCamera { get { return m_Cameras == null ? null : m_Cameras.SingleOrDefault(c => c.UsbId == m_SelectedUsbId); } }

		public CameraComponent(ZoomRoom parent) : base(parent)
		{
			Subscribe(parent);
		}

		protected override void DisposeFinal()
		{
			base.DisposeFinal();

			Unsubscribe(Parent);
		}

		#region Methods

		protected override void Initialize()
		{
			Parent.SendCommand("zConfiguration Video Camera selectedId");
			Parent.SendCommand("zStatus Video Camera Line");
		}

		public IEnumerable<CameraInfo> GetCameras()
		{
			return m_Cameras == null ? Enumerable.Empty<CameraInfo>() : m_Cameras.ToArray(m_Cameras.Length);
		}

		public void SetNearCameraAsVideoSource(int address)
		{
			address = MathUtils.Clamp(address, 0, m_Cameras.Length - 1);
			SetActiveCameraByUsbId(m_Cameras[address].UsbId);
		}

		public void SetActiveCameraByUsbId(string usbId)
		{
			Parent.SendCommand("zConfiguration Video Camera selectedId: {0}", usbId);
		}

		#endregion

		#region Parent Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<VideoCameraLineResponse>(CameraListCallback);
			parent.RegisterResponseCallback<VideoConfigurationResponse>(SelectedCameraCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<VideoCameraLineResponse>(CameraListCallback);
			parent.UnregisterResponseCallback<VideoConfigurationResponse>(SelectedCameraCallback);
		}

		private void CameraListCallback(ZoomRoom zoomroom, VideoCameraLineResponse response)
		{
			m_Cameras = response.Cameras;
			OnCamerasUpdated.Raise(this);
		}

		private void SelectedCameraCallback(ZoomRoom zoomRoom, VideoConfigurationResponse response)
		{
			m_SelectedUsbId = response.Video.Camera.SelectedId;
			OnActiveCameraUpdated.Raise(this);
		}

		#endregion
	}
}