using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public abstract class AbstractZoomRoomResponseConverter<T> : AbstractGenericJsonConverter<T>
		where T : AbstractZoomRoomResponse
	{
		private const string ATTR_TOP_KEY = "topKey";
		private const string ATTR_TYPE = "type";
		private const string ATTR_SYNC = "Sync";
		private const string ATTR_STATUS = "Status";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, T value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.TopKey != null)
				writer.WriteProperty(ATTR_TOP_KEY, value.TopKey);

			if (value.Type != default(eZoomRoomApiType))
				writer.WriteProperty(ATTR_TYPE, value.Type);

			if (value.Sync)
				writer.WriteProperty(ATTR_SYNC, value.Sync);

			if (value.Status != null)
			{
				writer.WritePropertyName(ATTR_STATUS);
				serializer.Serialize(writer, value.Status);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, T instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_TOP_KEY:
					instance.TopKey = reader.GetValueAsString();
					break;

				case ATTR_TYPE:
					instance.Type = reader.GetValueAsEnum<eZoomRoomApiType>();
					break;

				case ATTR_SYNC:
					instance.Sync = reader.GetValueAsBool();
					break;

				case ATTR_STATUS:
					instance.Status = serializer.Deserialize<ZoomRoomResponseStatus>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
