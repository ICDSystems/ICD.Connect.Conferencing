﻿using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("NeedWaitForHost", eZoomRoomApiType.zEvent, false),
	 ZoomRoomApiResponse("NeedWaitForHost", eZoomRoomApiType.zEvent, true)]
	[JsonConverter(typeof(NeedWaitForHostResponseConverter))]
	public sealed class NeedWaitForHostResponse : AbstractZoomRoomResponse
	{
		public NeedWaitForHost Response { get; set; }
	}

	[JsonConverter(typeof(NeedWaitForHostConverter))]
	public sealed class NeedWaitForHost
	{
		public bool Wait { get; set; }
	}
}