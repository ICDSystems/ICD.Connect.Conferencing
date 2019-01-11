using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Call", eZoomRoomApiType.zConfiguration, false),
	 ZoomRoomApiResponse("Call", eZoomRoomApiType.zConfiguration, true)]
	public sealed class CallConfigurationResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Call")]
		public CallConfiguration CallConfiguration { get; private set; }
	}

	public sealed class CallConfiguration
	{
		[JsonProperty("Microphone")]
		public MuteConfiguration Microphone { get; private set; }

		[JsonProperty("Camera")]
		public MuteConfiguration Camera { get; private set; }
	}

	public sealed class MuteConfiguration
	{
		[JsonProperty("Mute")]
		public bool Mute { get; private set; }
	}
}