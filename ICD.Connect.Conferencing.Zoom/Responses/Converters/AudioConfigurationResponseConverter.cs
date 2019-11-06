using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class AudioConfigurationResponseConverter : AbstractZoomRoomResponseConverter<AudioConfigurationResponse>
	{
		private const string ATTR_AUDIO = "Audio";


		protected override void WriteProperties(JsonWriter writer, AudioConfigurationResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.AudioInputConfiguration != null)
			{
				writer.WritePropertyName(ATTR_AUDIO);
				serializer.Serialize(writer, value.AudioInputConfiguration);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, AudioConfigurationResponse instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_AUDIO:
					instance.AudioInputConfiguration = serializer.Deserialize<AudioInputConfiguration>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class AudioInputConfigurationConverter : AbstractGenericJsonConverter<AudioInputConfiguration>
	{
		private const string ATTR_INPUT = "Input";

		protected override void WriteProperties(JsonWriter writer, AudioInputConfiguration value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.InputConfiguration != null)
			{
				writer.WritePropertyName(ATTR_INPUT);
				serializer.Serialize(writer, value.InputConfiguration);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, AudioInputConfiguration instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_INPUT:
					instance.InputConfiguration = serializer.Deserialize<InputConfiguration>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class InputConfigurationConverter : AbstractGenericJsonConverter<InputConfiguration>
	{
		private const string ATTR_IS_SAP_DISABLED = "is_sap_disabled";
		private const string ATTR_REDUCE_REVERB = "reduce_reverb";

		protected override void WriteProperties(JsonWriter writer, InputConfiguration value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value != null)
				writer.WriteProperty(ATTR_IS_SAP_DISABLED, serializer);

			if (value != null)
				writer.WriteProperty(ATTR_REDUCE_REVERB, serializer);
		}

		protected override void ReadProperty(string property, JsonReader reader, InputConfiguration instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_IS_SAP_DISABLED:
					instance.IsSapDisabled = reader.GetValueAsBool();
					break;
				case ATTR_REDUCE_REVERB:
					instance.ReduceReverb = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
