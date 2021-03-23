using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Camera;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class VideoCameraLineResponseConverter : AbstractZoomRoomResponseConverter<VideoCameraLineResponse>
	{
		private const string ATTR_VIDEO_CAMERA_LINE = "Video Camera Line";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, VideoCameraLineResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Cameras != null && value.Cameras.Length > 0)
			{
				writer.WritePropertyName(ATTR_VIDEO_CAMERA_LINE);
				serializer.SerializeArray(writer, value.Cameras);
			}
		}

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