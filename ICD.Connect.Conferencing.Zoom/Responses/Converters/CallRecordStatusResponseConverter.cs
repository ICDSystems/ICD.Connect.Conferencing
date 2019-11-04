using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class CallRecordStatusResponseConverter : AbstractZoomRoomResponseConverter<CallRecordStatusResponse>
	{
		private const string ATTR_CALL_RECORD = "CallRecord";

		protected override void WriteProperties(JsonWriter writer, CallRecordStatusResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CallRecord != null)
			{
				writer.WritePropertyName(ATTR_CALL_RECORD);
				serializer.Serialize(writer, value);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, CallRecordStatusResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALL_RECORD:
					instance.CallRecord = serializer.Deserialize<CallRecord>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}

		}
	}
}
