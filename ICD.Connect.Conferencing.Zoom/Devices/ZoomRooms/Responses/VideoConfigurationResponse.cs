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

		public bool HideConferenceSelfVideo { get; set; }
	}

	[JsonConverter(typeof(VideoCameraConfigurationConverter))]
	public class VideoCameraConfiguration
	{
		[CanBeNull]
		public string SelectedId { get; set; }
	}
}
