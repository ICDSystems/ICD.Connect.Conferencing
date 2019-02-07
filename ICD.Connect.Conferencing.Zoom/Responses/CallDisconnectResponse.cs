using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("CallDisconnect", eZoomRoomApiType.zEvent, false)]
	public sealed class CallDisconnectResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("CallDisconnect")]
		public CallDisconnect Disconnect { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			Disconnect = new CallDisconnect();
			Disconnect.LoadFromJObject((JObject) jObject["CallDisconnect"]);
		}
	}

	public sealed class CallDisconnect : AbstractZoomRoomData
	{
		[JsonProperty("success")]
		public eZoomBoolean Success { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			Success = jObject["success"].ToObject<eZoomBoolean>();
		}
	}
}