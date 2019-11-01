using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public class VideoUnMuteRequestResponseConverter : AbstractGenericJsonConverter<VideoUnMuteRequestResponse>
	{
		private const string ATTR_VIDEO_UNMUTE_REQUEST = "VideoUnMuteRequest";

		protected override void WriteProperties(JsonWriter writer, VideoUnMuteRequestResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.VideoUnMuteRequest != null)
			{
				writer.WritePropertyName(ATTR_VIDEO_UNMUTE_REQUEST);
				serializer.Serialize(writer, value.VideoUnMuteRequest);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, VideoUnMuteRequestResponse instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_VIDEO_UNMUTE_REQUEST:
					instance.VideoUnMuteRequest = serializer.Deserialize<VideoUnMuteRequestEvent>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class VideoUnMuteRequestEventConverter : AbstractGenericJsonConverter<VideoUnMuteRequestEvent>
	{
		private const string ATTR_ID = "ID";
		private const string ATTR_IS_COHOST = "isCoHost";
		private const string ATTR_IS_HOST = "isHost";

		protected override void WriteProperties(JsonWriter writer, VideoUnMuteRequestEvent value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if(value.Id != null)
				writer.WriteProperty(ATTR_ID, value.Id);

			if(value.IsCoHost)
				writer.WriteProperty(ATTR_IS_COHOST, value.IsCoHost);

			if(value.IsHost)
				writer.WriteProperty(ATTR_IS_HOST, value.IsHost);
		}

		protected override void ReadProperty(string property, JsonReader reader, VideoUnMuteRequestEvent instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_ID:
					instance.Id = reader.GetValueAsString();
					break;
				case ATTR_IS_COHOST:
					instance.IsCoHost = reader.GetValueAsBool();
					break;
				case ATTR_IS_HOST:
					instance.IsHost = reader.GetValueAsBool();
					break;
				
				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
