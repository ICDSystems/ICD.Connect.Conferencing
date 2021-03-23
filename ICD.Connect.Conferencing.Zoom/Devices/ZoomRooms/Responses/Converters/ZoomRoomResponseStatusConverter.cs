using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class ZoomRoomResponseStatusConverter : AbstractGenericJsonConverter<ZoomRoomResponseStatus>
	{
		private const string ATTR_MESSAGE = "message";
		private const string ATTR_STATE = "state";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, ZoomRoomResponseStatus value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Message != null)
				writer.WriteProperty(ATTR_MESSAGE, value.Message);

			if (value.State != default(eZoomRoomResponseState))
				writer.WriteProperty(ATTR_STATE, value.State);
		}

		protected override void ReadProperty(string property, JsonReader reader, ZoomRoomResponseStatus instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_MESSAGE:
					instance.Message = reader.GetValueAsString();
					break;

				case ATTR_STATE:
					instance.State = reader.GetValueAsEnum<eZoomRoomResponseState>();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}