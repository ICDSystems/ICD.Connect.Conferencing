using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Call", eZoomRoomApiType.zConfiguration, false),
	 ZoomRoomApiResponse("Call", eZoomRoomApiType.zConfiguration, true)]
	[JsonConverter(typeof(CallConfigurationResponseConverter))]
	public sealed class CallConfigurationResponse : AbstractZoomRoomResponse
	{
		public CallConfiguration CallConfiguration { get; set; }
	}

	[JsonConverter(typeof(CallConfigurationConverter))]
	public sealed class CallConfiguration
	{
		public MuteConfiguration Microphone { get; set; }

		public MuteConfiguration Camera { get; set; }

		public LockConfiguration CallLockStatus { get; set; }
	}

	[JsonConverter(typeof(MuteConfigurationConverter))]
	public sealed class MuteConfiguration
	{
		public bool Mute { get; set; }
	}

	[JsonConverter(typeof(LockConfigurationConverter))]
	public sealed class LockConfiguration
	{
		public bool Lock { get; set; }
	}
}
