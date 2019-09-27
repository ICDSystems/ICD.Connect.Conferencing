using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("UpdateCallRecordInfo", eZoomRoomApiType.zEvent, true),
	 ZoomRoomApiResponse("UpdateCallRecordInfo", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(UpdatedCallRecordInfoResponseConverter))]
	public sealed class UpdatedCallRecordInfoResponse : AbstractZoomRoomResponse
	{
		public UpdateCallRecordInfoEvent callRecordInfo { get; set; }
	}

	[JsonConverter(typeof(UpdatedCallRecordInfoEventConverter))]
	public sealed class UpdateCallRecordInfoEvent
	{
		public bool CanRecord { get; set; }
		public bool EmailRequired { get; set; }
		public bool AmIRecording { get; set; }
		public bool MeetingsIsBeingRecorded { get; set; }
	}
}
