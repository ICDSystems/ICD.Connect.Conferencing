#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("UpdateCallRecordInfo", eZoomRoomApiType.zEvent, true),
	 ZoomRoomApiResponse("UpdateCallRecordInfo", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(UpdatedCallRecordInfoResponseConverter))]
	public sealed class UpdatedCallRecordInfoResponse : AbstractZoomRoomResponse
	{
		[CanBeNull]
		public UpdateCallRecordInfoEvent CallRecordInfo { get; set; }
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
