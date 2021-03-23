using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class NeedWaitForHostResponseConverter : AbstractZoomRoomResponseConverter<NeedWaitForHostResponse>
	{
		private const string ATTR_NEED_WAIT_FOR_HOST = "NeedWaitForHost";

		protected override void WriteProperties(JsonWriter writer, NeedWaitForHostResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Response != null)
			{
				writer.WritePropertyName(ATTR_NEED_WAIT_FOR_HOST);
				serializer.Serialize(writer, value.Response);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, NeedWaitForHostResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_NEED_WAIT_FOR_HOST:
					instance.Response = serializer.Deserialize<NeedWaitForHost>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class NeedWaitForHostConverter : AbstractGenericJsonConverter<NeedWaitForHost>
	{
		private const string ATTR_WAIT = "Wait";

		protected override void WriteProperties(JsonWriter writer, NeedWaitForHost value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Wait)
			{
				writer.WritePropertyName(ATTR_WAIT);
				serializer.Serialize(writer, value.Wait);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, NeedWaitForHost instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_WAIT:
					instance.Wait = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
