using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	public enum eZoomRoomResponseState
	{
		OK,
		Error
	}

	[JsonConverter(typeof(ZoomRoomResponseStatusConverter))]
	public sealed class ZoomRoomResponseStatus
	{
		public string Message { get; set; }

		public eZoomRoomResponseState State { get; set; }
	}
}