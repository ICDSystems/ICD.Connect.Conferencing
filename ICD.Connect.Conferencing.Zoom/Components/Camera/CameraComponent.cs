using ICD.Common.Utils;
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

		#region Methods

		protected override void Initialize()
		{
			//Parent.SendCommand("zStatus Video Camera Line");
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