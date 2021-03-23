using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("Audio", eZoomRoomApiType.zConfiguration, false),
	 ZoomRoomApiResponse("Audio", eZoomRoomApiType.zConfiguration, true)]
	[JsonConverter(typeof(AudioConfigurationResponseConverter))]
	public sealed class AudioConfigurationResponse : AbstractZoomRoomResponse
	{
		[CanBeNull]
		public AudioConfiguration AudioConfiguration { get; set; }
	}

	[JsonConverter(typeof(AudioConfigurationConverter))]
	public sealed class AudioConfiguration
	{
		[CanBeNull]
		public InputConfiguration InputConfiguration { get; set; }

		[CanBeNull]
		public OutputConfiguration OutputConfiguration { get; set; }
	}

	[JsonConverter(typeof(InputConfigurationConverter))]
	public sealed class InputConfiguration
	{
		public bool? IsSapDisabled { get; set; }

		public bool? ReduceReverb { get; set; }

		public int? Volume { get; set; }

		[CanBeNull]
		public string SelectedId { get; set; }
	}

	[JsonConverter(typeof(OutputConfigurationConverter))]
	public sealed class OutputConfiguration
	{
		public int? Volume { get; set; }

		[CanBeNull]
		public string SelectedId { get; set; }
	}
}
