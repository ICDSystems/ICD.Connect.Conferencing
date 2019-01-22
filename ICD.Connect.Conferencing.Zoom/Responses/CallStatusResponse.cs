using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Call", eZoomRoomApiType.zStatus, true),
	 ZoomRoomApiResponse("Call", eZoomRoomApiType.zStatus, false)]
	public sealed class CallStatusResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Call")]
		public CallStatusInfo CallStatus { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			CallStatus = new CallStatusInfo();
			CallStatus.LoadFromJObject((JObject) jObject["Call"]);
		}
	}

	public sealed class CallStatusInfo : AbstractZoomRoomData
	{
		[JsonProperty("Status")]
		public eCallStatus? Status { get; private set; }

		[JsonProperty("ClosedCaption")]
		public ClosedCaption ClosedCaption { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			Status = jObject["Status"] == null ? (eCallStatus?)null : jObject["Status"].ToObject<eCallStatus>();

			if (jObject["ClosedCaption"] != null)
			{
				ClosedCaption = new ClosedCaption();
				ClosedCaption.LoadFromJObject((JObject)jObject["ClosedCaption"]);
			}
		}
	}

	public sealed class ClosedCaption : AbstractZoomRoomData
	{
		[JsonProperty("Available")]
		public bool Available { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			Available = jObject["Available"].ToObject<bool>();
		}
	}

	public enum eCallStatus
	{
		UNKNOWN,
		NOT_IN_MEETING,
		CONNECTING_MEETING,
		IN_MEETING,
		LOGGED_OUT
	}
}