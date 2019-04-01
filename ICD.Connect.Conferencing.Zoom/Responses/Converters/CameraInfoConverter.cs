using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Camera;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class CameraInfoConverter : AbstractGenericJsonConverter<CameraInfo>
	{
		private const string ATTR_ALIAS = "Alias";
		private const string ATTR_NAME = "Name";
		private const string ATTR_USB_ID = "id";

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