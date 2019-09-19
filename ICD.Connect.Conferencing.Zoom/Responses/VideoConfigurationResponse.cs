using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Video", eZoomRoomApiType.zConfiguration, true),
	 ZoomRoomApiResponse("Video", eZoomRoomApiType.zConfiguration, false)]
	[JsonConverter(typeof(VideoConfigurationResponseConverter))]
	public sealed class VideoConfigurationResponse : AbstractZoomRoomResponse
	{
		[CanBeNull]
		public VideoConfiguration Video { get; set; }
	}

	[JsonConverter(typeof(VideoConfigurationConverter))]
	public sealed class VideoConfiguration
	{
		[CanBeNull]
		public VideoCameraConfiguration Camera { get; set; }
	}

	[JsonConverter(typeof(VideoCameraConfigurationConverter))]
	public class VideoCameraConfiguration
	{
		[CanBeNull]
		public string SelectedId { get; set; }
	}
}
