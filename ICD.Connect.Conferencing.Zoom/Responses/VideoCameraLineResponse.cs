using System.Linq;
using ICD.Connect.Conferencing.Zoom.Components.Camera;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Video Camera Line", eZoomRoomApiType.zStatus, true),
	 ZoomRoomApiResponse("Video Camera Line", eZoomRoomApiType.zStatus, false)]
	public class VideoCameraLineResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Video Camera Line")]
		public CameraInfo[] Cameras { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			Cameras = jObject["Video Camera Line"].Children().Select(o =>
			{
				var camera = new CameraInfo();
				camera.LoadFromJObject((JObject) o);
				return camera;
			}).ToArray();
		}
	}
}