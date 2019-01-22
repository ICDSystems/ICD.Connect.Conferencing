using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Call", eZoomRoomApiType.zConfiguration, false),
	 ZoomRoomApiResponse("Call", eZoomRoomApiType.zConfiguration, true)]
	public sealed class CallConfigurationResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Call")]
		public CallConfiguration CallConfiguration { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			CallConfiguration = new CallConfiguration();
			CallConfiguration.LoadFromJObject((JObject)jObject["Call"]);
		}
	}

	public sealed class CallConfiguration : AbstractZoomRoomData
	{
		[JsonProperty("Microphone")]
		public MuteConfiguration Microphone { get; private set; }

		[JsonProperty("Camera")]
		public MuteConfiguration Camera { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			if (jObject["Microphone"] != null)
			{
				Microphone = new MuteConfiguration();
				Microphone.LoadFromJObject((JObject) jObject["Microphone"]);
			}

			if (jObject["Camera"] != null)
			{
				Camera = new MuteConfiguration();
				Camera.LoadFromJObject((JObject)jObject["Camera"]);
			}
		}
	}

	public sealed class MuteConfiguration : AbstractZoomRoomData
	{
		[JsonProperty("Mute")]
		public bool Mute { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			Mute = jObject["Mute"].ToObject<bool>();
		}
	}
}