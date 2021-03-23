using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class PinStatusOfScreenNotificationResponseConverter : AbstractZoomRoomResponseConverter<PinStatusOfScreenNotificationResponse>
	{
		private const string ATTR_PIN_STATUS_OF_SCREEN_NOTIFICATION = "PinStatusOfScreenNotification";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, PinStatusOfScreenNotificationResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.PinStatusOfScreenNotification != null)
			{
				writer.WritePropertyName(ATTR_PIN_STATUS_OF_SCREEN_NOTIFICATION);
				serializer.Serialize(writer, value.PinStatusOfScreenNotification);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, PinStatusOfScreenNotificationResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_PIN_STATUS_OF_SCREEN_NOTIFICATION:
					instance.PinStatusOfScreenNotification = serializer.Deserialize<PinStatusOfScreenNotification>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
