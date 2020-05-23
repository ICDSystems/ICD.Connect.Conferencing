﻿using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
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
	public sealed class AudioInputLine
	{
		[CanBeNull]
		public string Alias { get; set; }

		[CanBeNull]
		public string Name { get; set; }

		public bool? Selected { get; set; }

		public bool? CombinedDevice { get; set; }

		[CanBeNull]
		public string Id { get; set; }

		public bool? ManuallySelected { get; set; }

		public int? NumberOfCombinedDevices { get; set; }

		public int? PtzComId { get; set; }
	}
}
