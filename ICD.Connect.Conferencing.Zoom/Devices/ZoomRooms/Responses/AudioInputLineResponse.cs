﻿#if NETFRAMEWORK
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
	[ZoomRoomApiResponse("Audio Input Line", eZoomRoomApiType.zStatus, false),
	 ZoomRoomApiResponse("Audio Input Line", eZoomRoomApiType.zStatus, true)]
	[JsonConverter(typeof(AudioInputLineResponseConverter))]
	public sealed class AudioInputLineResponse : AbstractZoomRoomResponse
	{
		[CanBeNull]
		public AudioInputLine[] AudioInputLines { get; set; }
	}

	[JsonConverter(typeof(AudioInputLineConverter))]
	public sealed class AudioInputLine : AbstractAudioInputOutputLine
	{
	}
}
