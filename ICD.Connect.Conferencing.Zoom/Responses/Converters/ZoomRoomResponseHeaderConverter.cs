using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class ZoomRoomResponseHeaderConverter : AbstractGenericJsonConverter<ZoomRoomResponseHeader>
	{
		private const string ATTR_TOP_KEY = "topKey";
		private const string ATTR_TYPE = "type";
		private const string ATTR_SYNC = "Sync";
		private const string ATTR_STATUS = "Status";

		protected override void ReadProperty(string property, JsonReader reader, ZoomRoomResponseHeader instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_TOP_KEY:
					instance.TopKey = reader.GetValueAsString();
					break;

				case ATTR_TYPE:
					instance.Type = reader.GetValueAsEnum<eZoomRoomApiType>();
					break;

				case ATTR_SYNC:
					instance.Sync = reader.GetValueAsBool();
					break;

				case ATTR_STATUS:
					instance.Status = serializer.Deserialize<ZoomRoomResponseStatus>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
