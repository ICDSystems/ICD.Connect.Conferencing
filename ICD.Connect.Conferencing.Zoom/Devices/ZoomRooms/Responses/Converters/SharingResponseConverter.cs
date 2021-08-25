#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class SharingResponseConverter : AbstractZoomRoomResponseConverter<SharingResponse>
	{
		private const string ATTR_SHARING = "Sharing";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, SharingResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Sharing != null)
			{
				writer.WritePropertyName(ATTR_SHARING);
				serializer.Serialize(writer, value.Sharing);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, SharingResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SHARING:
					instance.Sharing = serializer.Deserialize<SharingInfo>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}