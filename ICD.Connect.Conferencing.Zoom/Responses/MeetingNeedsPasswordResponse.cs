using System;
using System.Collections.Generic;
using System.Text;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("MeetingNeedsPassword", eZoomRoomApiType.zEvent, true),
	 ZoomRoomApiResponse("MeetingNeedsPassword", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(MeetingNeedsPasswordResponseConverter))]
	public sealed class MeetingNeedsPasswordResponse : AbstractZoomRoomResponse
	{
		public MeetingNeedsPasswordEvent MeetingNeedsPassword { get; set; }
	}

	[JsonConverter(typeof(MeetingNeedsPasswordEventConverter))]
	public sealed class MeetingNeedsPasswordEvent
	{
		public bool NeedsPassword { get; set; }
		public bool WrongAndRetry { get; set; }
	}
}
