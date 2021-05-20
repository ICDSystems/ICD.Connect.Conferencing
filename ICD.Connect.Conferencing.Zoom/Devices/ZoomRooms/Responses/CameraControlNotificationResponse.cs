using System;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("CameraControlNotification", eZoomRoomApiType.zEvent, false), 
	 ZoomRoomApiResponse("CameraControlNotification", eZoomRoomApiType.zEvent, true)]
	[JsonConverter(typeof(CameraControlNotificationResponseConverter))]
	public sealed class CameraControlNotificationResponse : AbstractZoomRoomResponse
	{
		public CameraControlNotification CameraControlNotification { get; set; }
	}

	[JsonConverter(typeof(CameraControlNotificationConverter))]
	public sealed class CameraControlNotification : EventArgs
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