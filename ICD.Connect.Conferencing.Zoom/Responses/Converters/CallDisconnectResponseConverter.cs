using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class CallDisconnectResponseConverter : AbstractGenericJsonConverter<CallDisconnectResponse>
	{
		private const string ATTR_CALL_DISCONNECT = "CallDisconnect";

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
