using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class CallStatusResponseConverter : AbstractGenericJsonConverter<CallStatusResponse>
	{
		private const string ATTR_CALL = "Call";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CallStatusResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CallStatus != null)
			{
				writer.WritePropertyName(ATTR_CALL);
				serializer.Serialize(writer, value.CallStatus);
			}
		}

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

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CallStatusInfo value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Status != null)
				writer.WriteProperty(ATTR_STATUS, value.Status.Value);

			if (value.ClosedCaption != null)
			{
				writer.WritePropertyName(ATTR_CLOSED_CAPTION);
				serializer.Serialize(writer, value.ClosedCaption);
			}
		}

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

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, ClosedCaption value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Available)
				writer.WriteProperty(ATTR_AVAILABLE, value.Available);
		}

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
