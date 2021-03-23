using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class AudioConfigurationResponseConverter : AbstractZoomRoomResponseConverter<AudioConfigurationResponse>
	{
		private const string ATTR_AUDIO = "Audio";


		protected override void WriteProperties(JsonWriter writer, AudioConfigurationResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.AudioConfiguration != null)
			{
				writer.WritePropertyName(ATTR_AUDIO);
				serializer.Serialize(writer, value.AudioConfiguration);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, AudioConfigurationResponse instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_AUDIO:
					instance.AudioConfiguration = serializer.Deserialize<AudioConfiguration>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class AudioConfigurationConverter : AbstractGenericJsonConverter<AudioConfiguration>
	{
		private const string ATTR_INPUT = "Input";
		private const string ATTR_OUTPUT = "Output";

		protected override void WriteProperties(JsonWriter writer, AudioConfiguration value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.InputConfiguration != null)
			{
				writer.WritePropertyName(ATTR_INPUT);
				serializer.Serialize(writer, value.InputConfiguration);
			}

			if (value.OutputConfiguration != null)
			{
				writer.WritePropertyName(ATTR_OUTPUT);
				serializer.Serialize(writer, value.OutputConfiguration);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, AudioConfiguration instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_INPUT:
					instance.InputConfiguration = serializer.Deserialize<InputConfiguration>(reader);
					break;
				case ATTR_OUTPUT:
					instance.OutputConfiguration = serializer.Deserialize<OutputConfiguration>(reader);
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
		private const string ATTR_VOLUME = "volume";
		private const string ATTR_SELECTED_ID = "selectedId";

		protected override void WriteProperties(JsonWriter writer, InputConfiguration value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.IsSapDisabled != null)
				writer.WriteProperty(ATTR_IS_SAP_DISABLED, serializer);

			if (value.ReduceReverb != null)
				writer.WriteProperty(ATTR_REDUCE_REVERB, serializer);

			if (value.Volume != null)
				writer.WriteProperty(ATTR_VOLUME, serializer);

			if (value.SelectedId != null)
				writer.WriteProperty(ATTR_SELECTED_ID, serializer);
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
				case ATTR_VOLUME:
					instance.Volume = reader.GetValueAsInt();
					break;
				case ATTR_SELECTED_ID:
					instance.SelectedId = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class OutputConfigurationConverter : AbstractGenericJsonConverter<OutputConfiguration>
	{
		private const string ATTR_VOLUME = "volume";
		private const string ATTR_SELECTED_ID = "selectedId";

		protected override void WriteProperties(JsonWriter writer, OutputConfiguration value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Volume != null)
				writer.WriteProperty(ATTR_VOLUME, serializer);

			if (value.SelectedId != null)
				writer.WriteProperty(ATTR_SELECTED_ID, serializer);
		}

		protected override void ReadProperty(string property, JsonReader reader, OutputConfiguration instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_VOLUME:
					instance.Volume = reader.GetValueAsInt();
					break;
				case ATTR_SELECTED_ID:
					instance.SelectedId = reader.GetValueAsString();
					break;


				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
