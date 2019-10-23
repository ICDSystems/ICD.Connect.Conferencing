using ICD.Common.Properties;
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
		[CanBeNull]
		public MuteConfiguration Microphone { get; set; }

		[CanBeNull]
		public MuteConfiguration Camera { get; set; }

		[CanBeNull]
		public LockConfiguration CallLockStatus { get; set; }

		[CanBeNull]
		public CallLayoutConfigurationQuery Layout { get; set; }

		[CanBeNull]
		public MuteUserOnEntryConfiguration MuteUserOnEntry { get; set; }
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

	[JsonConverter(typeof(CallLayoutConfigurationQueryConverter))]
	public sealed class CallLayoutConfigurationQuery
	{
		public eZoomLayoutSize Size { get; set; }
		public eZoomLayoutPosition Position { get; set; }
	}

	[JsonConverter(typeof(MuteUserOnEntryConfigurationConverter))]
	public class MuteUserOnEntryConfiguration
	{
		public bool Enabled { get; set; }
	}
}
