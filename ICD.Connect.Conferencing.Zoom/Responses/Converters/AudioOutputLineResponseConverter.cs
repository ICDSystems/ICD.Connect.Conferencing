using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class AudioOutputLineResponseConverter : AbstractZoomRoomResponseConverter<AudioOutputLineResponse>
	{
		private const string ATTR_AUDIO_OUTPUT_LINE = "Audio Output Line";

		protected override void WriteProperties(JsonWriter writer, AudioOutputLineResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.AudioOutputLines != null)
			{
				writer.WritePropertyName(ATTR_AUDIO_OUTPUT_LINE);
				serializer.Serialize(writer, value.AudioOutputLines);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, AudioOutputLineResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_AUDIO_OUTPUT_LINE:
					instance.AudioOutputLines = serializer.DeserializeArray<AudioOutputLine>(reader).ToArray();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class AudioOutputLineConverter : AbstractGenericJsonConverter<AudioOutputLine>
	{
		private const string ATTR_ALIAS = "Alias";
		private const string ATTR_NAME = "Name";
		private const string ATTR_SELECTED = "Selected";
		private const string ATTR_COMBINED_DEVICE = "combinedDevice";
		private const string ATTR_ID = "id";
		private const string ATTR_MANUALLYSELECTED = "manuallySelected";
		private const string ATTR_NUMBER_OF_COMBINED_DEVICES = "numberOfCombinedDevices";
		private const string ATTR_PTZ_COM_ID = "ptzComId";

		protected override void WriteProperties(JsonWriter writer, AudioOutputLine value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Alias != null)
			{
				writer.WritePropertyName(ATTR_ALIAS);
				serializer.Serialize(writer, value.Alias);
			}

			if (value.Name != null)
			{
				writer.WritePropertyName(ATTR_NAME);
				serializer.Serialize(writer, value.Name);
			}

			if (value.Selected != null)
			{
				writer.WritePropertyName(ATTR_SELECTED);
				serializer.Serialize(writer, value.Selected);
			}

			if (value.CombinedDevice != null)
			{
				writer.WritePropertyName(ATTR_COMBINED_DEVICE);
				serializer.Serialize(writer, value.CombinedDevice);
			}

			if (value.Id != null)
			{
				writer.WritePropertyName(ATTR_ID);
				serializer.Serialize(writer, value.Id);
			}

			if (value.ManuallySelected != null)
			{
				writer.WritePropertyName(ATTR_MANUALLYSELECTED);
				serializer.Serialize(writer, value.ManuallySelected);
			}

			if (value.NumberOfCombinedDevices != null)
			{
				writer.WritePropertyName(ATTR_NUMBER_OF_COMBINED_DEVICES);
				serializer.Serialize(writer, value.NumberOfCombinedDevices);
			}

			if (value.PtzComId != null)
			{
				writer.WritePropertyName(ATTR_PTZ_COM_ID);
				serializer.Serialize(writer, value.PtzComId);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, AudioOutputLine instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_ALIAS:
					instance.Alias = reader.GetValueAsString();
					break;
				case ATTR_NAME:
					instance.Name = reader.GetValueAsString();
					break;
				case ATTR_SELECTED:
					instance.Selected = reader.GetValueAsBool();
					break;
				case ATTR_COMBINED_DEVICE:
					instance.CombinedDevice = reader.GetValueAsBool();
					break;
				case ATTR_ID:
					instance.Id = reader.GetValueAsString();
					break;
				case ATTR_MANUALLYSELECTED:
					instance.ManuallySelected = reader.GetValueAsBool();
					break;
				case ATTR_NUMBER_OF_COMBINED_DEVICES:
					instance.NumberOfCombinedDevices = reader.GetValueAsInt();
					break;
				case ATTR_PTZ_COM_ID:
					instance.PtzComId = reader.GetValueAsInt();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
