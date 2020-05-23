using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Audio Output Line", eZoomRoomApiType.zStatus, false),
	 ZoomRoomApiResponse("Audio Output Line", eZoomRoomApiType.zStatus, true)]
	[JsonConverter(typeof(AudioOutputLineResponseConverter))]
	public class AudioOutputLineResponse : AbstractZoomRoomResponse
	{
		[CanBeNull]
		public AudioOutputLine[] AudioOutputLines { get; set; }
	}

	[JsonConverter(typeof(AudioOutputLineConverter))]
	public sealed class AudioOutputLine
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
