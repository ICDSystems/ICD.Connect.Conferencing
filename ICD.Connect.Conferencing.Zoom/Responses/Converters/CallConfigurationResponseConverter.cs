using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class CallConfigurationResponseConverter : AbstractGenericJsonConverter<CallConfigurationResponse>
	{
		private const string ATTR_CALL = "Call";

		protected override void ReadProperty(string property, JsonReader reader, CallConfigurationResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CALL:
					instance.CallConfiguration = serializer.Deserialize<CallConfiguration>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class CallConfigurationConverter : AbstractGenericJsonConverter<CallConfiguration>
	{
		private const string ATTR_MICROPHONE = "Microphone";
		private const string ATTR_CAMERA = "Camera";

		protected override void ReadProperty(string property, JsonReader reader, CallConfiguration instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_MICROPHONE:
					instance.Microphone = serializer.Deserialize<MuteConfiguration>(reader);
					break;

				case ATTR_CAMERA:
					instance.Camera = serializer.Deserialize<MuteConfiguration>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class MuteConfigurationConverter : AbstractGenericJsonConverter<MuteConfiguration>
	{
		private const string ATTR_MUTE = "Mute";

		protected override void ReadProperty(string property, JsonReader reader, MuteConfiguration instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_MUTE:
					instance.Mute = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}