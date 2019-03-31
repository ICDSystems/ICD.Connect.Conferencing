using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Camera;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class VideoCameraLineResponseConverter : AbstractGenericJsonConverter<VideoCameraLineResponse>
	{
		private const string ATTR_VIDEO_CAMERA_LINE = "Video Camera Line";

		protected override void ReadProperty(string property, JsonReader reader, VideoCameraLineResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_VIDEO_CAMERA_LINE:
					instance.Cameras = serializer.DeserializeArray<CameraInfo>(reader).ToArray();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}