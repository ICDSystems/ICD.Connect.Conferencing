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
	public sealed class SharingInfoConverter : AbstractGenericJsonConverter<SharingInfo>
	{
		private const string ATTR_WIFI_NAME = "wifiName";
		private const string ATTR_SERVER_NAME = "serverName";
		private const string ATTR_PASSWORD = "password";
		private const string ATTR_IS_AIR_HOST_CLIENT_CONNECTED = "isAirHostClientConnected";
		private const string ATTR_IS_BLACK_MAGIC_CONNECTED = "isBlackMagicConnected";
		private const string ATTR_IS_BLACK_MAGIC_DATA_AVAILABLE = "isBlackMagicDataAvailable";
		private const string ATTR_IS_SHARING_BLACK_MAGIC = "isSharingBlackMagic";
		private const string ATTR_DIRECT_PRESENTATION_PAIRING_CODE = "directPresentationPairingCode";
		private const string ATTR_DIRECT_PRESENTATION_SHARING_KEY = "directPresentationSharingKey";
		private const string ATTR_IS_DIRECT_PRESENTATION_CONNECTED = "isDirectPresentationConnected";
		private const string ATTR_DISPLAY_STATE = "dispState";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, SharingInfo value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.IsBlackMagicConnected)
				writer.WriteProperty(ATTR_IS_BLACK_MAGIC_CONNECTED, value.IsBlackMagicConnected);

			if (value.IsBlackMagicDataAvailable)
				writer.WriteProperty(ATTR_IS_BLACK_MAGIC_DATA_AVAILABLE, value.IsBlackMagicDataAvailable);

			if (value.IsSharingBlackMagic)
				writer.WriteProperty(ATTR_IS_SHARING_BLACK_MAGIC, value.IsSharingBlackMagic);
		}

		protected override void ReadProperty(string property, JsonReader reader, SharingInfo instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_IS_BLACK_MAGIC_CONNECTED:
					instance.IsBlackMagicConnected = reader.GetValueAsBool();
					break;

				case ATTR_IS_BLACK_MAGIC_DATA_AVAILABLE:
					instance.IsBlackMagicDataAvailable = reader.GetValueAsBool();
					break;

				case ATTR_IS_SHARING_BLACK_MAGIC:
					instance.IsSharingBlackMagic = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
