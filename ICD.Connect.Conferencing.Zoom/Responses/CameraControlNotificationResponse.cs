using System;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("CameraControlNotification", eZoomRoomApiType.zEvent, false), 
	 ZoomRoomApiResponse("CameraControlNotification", eZoomRoomApiType.zEvent, true)]
	[JsonConverter(typeof(CameraControlNotificationResponseConverter))]
	public class CameraControlNotificationResponse : AbstractZoomRoomResponse
	{
		public CameraControlNotification CameraControlNotification { get; set; }
	}

	[JsonConverter(typeof(CameraControlNotificationConverter))]
	public class CameraControlNotification : EventArgs
	{
		public eCameraControlNotificationState State { get; set; }

		public string UserId { get; set; }

		public string UserName { get; set; }
	}

	public enum eCameraControlNotificationState
	{
		None = 0,
		ZRCCameraControlStateRequestedByFarEnd = 1,
		ZRCCameraControlStateGaveUpByFarEnd = 2,
		ZRCCameraControlStateControlRequestToRemote = 3
	}
}