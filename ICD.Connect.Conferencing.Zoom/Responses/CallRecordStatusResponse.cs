using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
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
