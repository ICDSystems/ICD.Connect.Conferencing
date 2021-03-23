using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("CallRecord", eZoomRoomApiType.zCommand, true),
	 ZoomRoomApiResponse("CallRecord", eZoomRoomApiType.zCommand, false)]
	[JsonConverter(typeof(CallRecordStatusResponseConverter))]
	public sealed class CallRecordStatusResponse : AbstractZoomRoomResponse
	{
		[CanBeNull]
		public CallRecord CallRecord { get; set; }
	}

	public sealed class CallRecord
	{
	}
}
