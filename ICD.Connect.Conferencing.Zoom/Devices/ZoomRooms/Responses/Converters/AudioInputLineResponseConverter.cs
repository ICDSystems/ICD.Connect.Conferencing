#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System.Linq;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class AudioInputLineResponseConverter : AbstractZoomRoomResponseConverter<AudioInputLineResponse>
	{
		private const string ATTR_AUDIO_INPUT_LINE = "Audio Input Line";

		protected override void WriteProperties(JsonWriter writer, AudioInputLineResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.AudioInputLines != null)
			{
				writer.WritePropertyName(ATTR_AUDIO_INPUT_LINE);
				serializer.Serialize(writer, value.AudioInputLines);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, AudioInputLineResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_AUDIO_INPUT_LINE:
					instance.AudioInputLines = serializer.DeserializeArray<AudioInputLine>(reader).ToArray();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class AudioInputLineConverter : AbstractAudioInputOutputLineConverter<AudioInputLine>
	{
	}
}
