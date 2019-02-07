using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	public abstract class AbstractZoomRoomData
	{
		public abstract void LoadFromJObject(JObject jObject);
	}

	// do not put a [JsonConverter(typeof(ZoomRoomResponseConverter)] attribute here, will infinite loop
	public abstract class AbstractZoomRoomResponse : AbstractZoomRoomData
	{
		private const string ATTR_TOP_KEY = "topKey";
		private const string ATTR_TYPE = "type";
		private const string ATTR_SYNC = "Sync";
		private const string ATTR_STATUS = "Status";

		/// <summary>
		/// Property key of where the actual response data is stored
		/// </summary>
		[JsonProperty("topKey")]
		public string TopKey { get; private set; }

		/// <summary>
		/// The type of response
		/// </summary>
		[JsonProperty("type")]
		public eZoomRoomApiType Type { get; private set; }

		/// <summary>
		/// Whether the command succeeded or not
		/// </summary>
		[JsonProperty("Status")]
		public ZoomRoomResponseStatus Status { get; private set; }

		/// <summary>
		/// Whether or not this is a synchronous response to a command (true)
		/// or asynchronous status update (false)
		/// </summary>
		[JsonProperty("Sync")]
		public bool Sync { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			TopKey = jObject[ATTR_TOP_KEY].ToString();
			Type = jObject[ATTR_TYPE].ToObject<eZoomRoomApiType>();
			Sync = jObject[ATTR_SYNC].ToObject<bool>();

			Status = new ZoomRoomResponseStatus();
			Status.LoadFromJObject((JObject)jObject[ATTR_STATUS]);
		}
	}

	public enum eZoomRoomApiType
	{
		zCommand,
		zConfiguration,
		zStatus,
		zEvent,
		zError
	}

	public class ZoomRoomResponseStatus : AbstractZoomRoomData
	{
		[JsonProperty("message")]
		public string Message { get; private set; }
		[JsonProperty("state")]
		public eZoomRoomResponseState State { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			Message = jObject["message"].ToString();
			State = jObject["state"].ToObject<eZoomRoomResponseState>();
		}
	}

	public enum eZoomRoomResponseState
	{
		OK,
		Error
	}
}