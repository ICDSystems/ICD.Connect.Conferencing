using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("IncomingCallIndication", eZoomRoomApiType.zEvent, false)]
	public sealed class IncomingCallResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("IncomingCallIndication")]
		public IncomingCall IncomingCall { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			IncomingCall = new IncomingCall();
			IncomingCall.LoadFromJObject((JObject) jObject["IncomingCallIndication"]);
		}
	}
}