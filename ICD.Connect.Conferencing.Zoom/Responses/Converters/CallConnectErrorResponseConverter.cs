using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class CallConnectErrorResponseConverter : AbstractGenericJsonConverter<CallConnectErrorResponse>
	{
		private const string ATTR_CALL_CONNECT_ERROR = "CallConnectError";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CallConnectErrorResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Error != null)
			{
				writer.WritePropertyName(ATTR_CALL_CONNECT_ERROR);
				serializer.Serialize(writer, value.Error);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, CallConnectErrorResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALL_CONNECT_ERROR:
					instance.Error = serializer.Deserialize<CallConnectError>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class CallConnectErrorConverter : AbstractGenericJsonConverter<CallConnectError>
	{
		private const string ATTR_ERROR_CODE = "error_code";
		private const string ATTR_ERROR_MESSAGE = "error_message";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CallConnectError value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.ErrorCode != 0)
				writer.WriteProperty(ATTR_ERROR_CODE, value.ErrorCode);

			if (value.ErrorMessage != null)
				writer.WriteProperty(ATTR_ERROR_MESSAGE, value.ErrorMessage);
		}

		protected override void ReadProperty(string property, JsonReader reader, CallConnectError instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_ERROR_CODE:
					instance.ErrorCode = reader.GetValueAsInt();
					break;

				case ATTR_ERROR_MESSAGE:
					instance.ErrorMessage = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}