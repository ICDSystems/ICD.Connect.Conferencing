using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class CallDisconnectResponseConverter : AbstractZoomRoomResponseConverter<CallDisconnectResponse>
	{
		private const string ATTR_CALL_DISCONNECT = "CallDisconnect";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CallDisconnectResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Disconnect != null)
			{
				writer.WritePropertyName(ATTR_CALL_DISCONNECT);
				serializer.Serialize(writer, value.Disconnect);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, CallDisconnectResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALL_DISCONNECT:
					instance.Disconnect = serializer.Deserialize<CallDisconnect>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class CallDisconnectConverter : AbstractGenericJsonConverter<CallDisconnect>
	{
		private const string ATTR_SUCCESS = "success";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CallDisconnect value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Success != default(eZoomBoolean))
				writer.WriteProperty(ATTR_SUCCESS, value.Success);
		}

		protected override void ReadProperty(string property, JsonReader reader, CallDisconnect instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SUCCESS:
					instance.Success = reader.GetValueAsEnum<eZoomBoolean>();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
