using System.Linq;
using ICD.Connect.Conferencing.Devices;

namespace ICD.Connect.Conferencing.Zoom.Components.Camera
{
	public class CameraComponent : AbstractZoomRoomComponent
	{
		public CameraComponent(ZoomRoom zoomRoom) : base(zoomRoom)
		{
			Subscribe(zoomRoom);
		}

		private void Subscribe(ZoomRoom zoomRoom)
		{
			throw new System.NotImplementedException();
		}

		public void SetNearCameraAsVideoSource(int address)
		{
			string usbId = ZoomRoom.InputIds.GetId(address);
			ZoomRoom.SendCommand(string.Format("zConfiguration Video Camera selectedId: {0}", usbId));
		}
	}
}