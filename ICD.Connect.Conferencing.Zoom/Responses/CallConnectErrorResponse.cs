using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("CallConnectError", eZoomRoomApiType.zEvent, false)]
	public sealed class CallConnectErrorResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("CallConnectError")]
		public CallConnectError Error { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			Error = new CallConnectError();
			Error.LoadFromJObject((JObject) jObject["CallConnectError"]);
		}
	}

	public sealed class CallConnectError : AbstractZoomRoomData
	{
		[JsonProperty("error_code")]
		public int ErrorCode { get; private set; }

		[JsonProperty("error_message")]
		public string ErrorMessage { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			ErrorCode = jObject["error_code"].ToObject<int>();
			ErrorMessage = jObject["error_message"].ToString();
		}
	}
}