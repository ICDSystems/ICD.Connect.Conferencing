using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class CallConnectErrorResponseConverter : AbstractGenericJsonConverter<CallConnectErrorResponse>
	{
		private const string ATTR_CALL_CONNECT_ERROR = "CallConnectError";

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