using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("VideoUnMuteRequest", eZoomRoomApiType.zEvent, true),
	 ZoomRoomApiResponse("VideoUnMuteRequest", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(VideoUnMuteRequestResponseConverter))]
	public sealed class VideoUnMuteRequestResponse : AbstractZoomRoomResponse
	{
		[CanBeNull]
		public  VideoUnMuteRequestEvent VideoUnMuteRequest { get; set; }
	}

	[JsonConverter(typeof(VideoUnMuteRequestEventConverter))]
	public sealed class VideoUnMuteRequestEvent
	{
			public string Id { get; set; }
			public bool IsCoHost { get; set; }
			public bool IsHost { get; set; }
	}
	
}
