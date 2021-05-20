using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("Audio Output Line", eZoomRoomApiType.zStatus, false),
	 ZoomRoomApiResponse("Audio Output Line", eZoomRoomApiType.zStatus, true)]
	[JsonConverter(typeof(AudioOutputLineResponseConverter))]
	public sealed class AudioOutputLineResponse : AbstractZoomRoomResponse
	{
		[CanBeNull]
		public AudioOutputLine[] AudioOutputLines { get; set; }
	}

	[JsonConverter(typeof(AudioOutputLineConverter))]
	public sealed class AudioOutputLine : AbstractAudioInputOutputLine
	{
	}
}
