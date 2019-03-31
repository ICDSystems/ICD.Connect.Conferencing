using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class ZoomRoomResponseStatusConverter : AbstractGenericJsonConverter<ZoomRoomResponseStatus>
	{
		private const string ATTR_MESSAGE = "message";
		private const string ATTR_STATE = "state";

		protected override void ReadProperty(string property, JsonReader reader, ZoomRoomResponseStatus instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_MESSAGE:
					instance.Message = reader.GetValueAsString();
					break;

				case ATTR_STATE:
					instance.State = reader.GetValueAsEnum<eZoomRoomResponseState>();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}