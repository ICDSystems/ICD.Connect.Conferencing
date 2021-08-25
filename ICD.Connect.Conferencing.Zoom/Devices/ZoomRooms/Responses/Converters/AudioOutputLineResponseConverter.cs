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

	public sealed class AudioOutputLineConverter : AbstractAudioInputOutputLineConverter<AudioOutputLine>
	{
	}
}
