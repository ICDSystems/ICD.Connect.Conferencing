using ICD.Connect.Conferencing.Zoom.Components.Presentation;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Sharing", eZoomRoomApiType.zStatus, true), 
	 ZoomRoomApiResponse("Sharing", eZoomRoomApiType.zStatus, false)]
	public sealed class SharingResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Sharing")]
		public SharingInfo Sharing { get; set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			Sharing = new SharingInfo();
			Sharing.LoadFromJObject((JObject) jObject["Sharing"]);
		}
	}
}