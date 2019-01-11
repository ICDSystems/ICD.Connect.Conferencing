using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Camera
{
	public class CameraComponent : AbstractZoomRoomComponent
	{
		private CameraInfo[] m_Cameras;

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
			Parent.SendCommand("zStatus Video Camera Line");
		}

		public IEnumerable<CameraInfo> GetCameras()
		{
			return m_Cameras == null ? Enumerable.Empty<CameraInfo>() : m_Cameras.ToArray(m_Cameras.Length);
		}

		public void SetNearCameraAsVideoSource(int address)
		{
			address = MathUtils.Clamp(address, 0, m_Cameras.Length - 1);
			Parent.SendCommand("zConfiguration Video Camera selectedId: {0}", m_Cameras[address].UsbId);
		}

		#endregion

		#region Parent Callbacks

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<VideoCameraLineResponse>(CameraListCallback);
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<VideoCameraLineResponse>(CameraListCallback);
		}

		private void CameraListCallback(ZoomRoom zoomroom, VideoCameraLineResponse response)
		{
			m_Cameras = response.Cameras;
		}

		#endregion
	}
}