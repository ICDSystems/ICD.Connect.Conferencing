#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class PinStatusOfScreenNotificationConverter : AbstractGenericJsonConverter<PinStatusOfScreenNotification>
	{
		private const string ATTR_SCREEN_INDEX = "screen_index";
		private const string ATTR_CAN_BE_PINNED = "can_be_pinned";
		private const string ATTR_CAN_PIN_SHARE = "can_pin_share";
		private const string ATTR_PINNED_USER_ID = "pinned_user_id";
		private const string ATTR_SCREEN_LAYOUT = "screen_layout";
		private const string ATTR_PINNED_SHARE_SOURCE_ID = "pinned_share_source_id";
		private const string ATTR_SHARE_SOURCE_TYPE = "share_source_type";
		private const string ATTR_WHY_CANNOT_PIN_SHARE = "why_cannot_pin_share";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, PinStatusOfScreenNotification value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.ScreenIndex != default(int))
				writer.WriteProperty(ATTR_SCREEN_INDEX, value.ScreenIndex);

			if (value.ScreenLayout != default(eZoomScreenLayout))
				writer.WriteProperty(ATTR_SCREEN_LAYOUT, value.ScreenLayout.ToUShort());
		}

		protected override void ReadProperty(string property, JsonReader reader, PinStatusOfScreenNotification instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SCREEN_INDEX:
					instance.ScreenIndex = reader.GetValueAsInt();
					break;

				case ATTR_SCREEN_LAYOUT:
					instance.ScreenLayout = (eZoomScreenLayout)(object)reader.GetValueAsInt();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
