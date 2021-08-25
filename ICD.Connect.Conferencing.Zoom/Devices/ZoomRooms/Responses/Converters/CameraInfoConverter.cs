#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Camera;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class CameraInfoConverter : AbstractGenericJsonConverter<CameraInfo>
	{
		private const string ATTR_ALIAS = "Alias";
		private const string ATTR_NAME = "Name";
		private const string ATTR_USB_ID = "id";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, CameraInfo value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Alias != null)
				writer.WriteProperty(ATTR_ALIAS, value.Alias);

			if (value.Name != null)
				writer.WriteProperty(ATTR_NAME, value.Name);

			if (value.UsbId != null)
				writer.WriteProperty(ATTR_USB_ID, value.UsbId);
		}

		protected override void ReadProperty(string property, JsonReader reader, CameraInfo instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_ALIAS:
					instance.Alias = reader.GetValueAsString();
					break;

				case ATTR_NAME:
					instance.Name = reader.GetValueAsString();
					break;

				case ATTR_USB_ID:
					instance.UsbId = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}