using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Camera
{
	public sealed class CameraControlNotificationEventArgs : GenericEventArgs<CameraControlNotification>
	{
		public CameraControlNotificationEventArgs(CameraControlNotification data) 
			: base(data)
		{
		}
	}
}