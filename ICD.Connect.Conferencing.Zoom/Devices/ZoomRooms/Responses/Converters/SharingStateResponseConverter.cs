#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class SharingStateResponseConverter : AbstractZoomRoomResponseConverter<SharingStateResponse>
	{
		private const string ATTR_SHARING_STATE = "SharingState";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, SharingStateResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.SharingState != null)
			{
				writer.WritePropertyName(ATTR_SHARING_STATE);
				serializer.Serialize(writer, value.SharingState);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, SharingStateResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SHARING_STATE:
					instance.SharingState = serializer.Deserialize<SharingStateInfo>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}