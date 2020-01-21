using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Camera
{
	public class CameraControlNotificationEventArgs : GenericEventArgs<CameraControlNotification>
	{
		public CameraControlNotificationEventArgs(CameraControlNotification data) 
			: base(data)
		{
		}
	}
}