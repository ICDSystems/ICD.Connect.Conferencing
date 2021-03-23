using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Call;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	/// <summary>
	/// Contains call info, received either as an event, status update, or command
	/// </summary>
	[ZoomRoomApiResponse("InfoResult", eZoomRoomApiType.zCommand, true),
	 ZoomRoomApiResponse("InfoResult", eZoomRoomApiType.zCommand, false)]
	[JsonConverter(typeof(InfoResultResponseConverter))]
	public sealed class InfoResultResponse : AbstractZoomRoomResponse
	{
		public CallInfo InfoResult { get; set; }
	}
}