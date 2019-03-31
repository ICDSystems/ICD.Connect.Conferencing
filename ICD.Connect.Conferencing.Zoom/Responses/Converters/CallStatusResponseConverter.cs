using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class CallStatusResponseConverter : AbstractGenericJsonConverter<CallStatusResponse>
	{
		private const string ATTR_CALL = "Call";

		protected override void ReadProperty(string property, JsonReader reader, CallStatusResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALL:
					instance.CallStatus = serializer.Deserialize<CallStatusInfo>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class CallStatusInfoConverter : AbstractGenericJsonConverter<CallStatusInfo>
	{
		private const string ATTR_STATUS = "Status";
		private const string ATTR_CLOSED_CAPTION = "ClosedCaption";

		protected override void ReadProperty(string property, JsonReader reader, CallStatusInfo instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_STATUS:
					instance.Status =
						reader.TokenType == JsonToken.Null
						? (eCallStatus?)null
						: reader.GetValueAsEnum<eCallStatus>();
					break;

				case ATTR_CLOSED_CAPTION:
					instance.ClosedCaption = serializer.Deserialize<ClosedCaption>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class ClosedCaptionConverter : AbstractGenericJsonConverter<ClosedCaption>
	{
		private const string ATTR_AVAILABLE = "Available";

		protected override void ReadProperty(string property, JsonReader reader, ClosedCaption instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_AVAILABLE:
					instance.Available = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
