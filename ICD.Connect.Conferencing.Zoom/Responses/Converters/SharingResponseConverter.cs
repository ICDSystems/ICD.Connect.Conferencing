using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Presentation;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class SharingResponseConverter : AbstractGenericJsonConverter<SharingResponse>
	{
		private const string ATTR_SHARING = "Sharing";

		protected override void ReadProperty(string property, JsonReader reader, SharingResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SHARING:
					instance.Sharing = serializer.Deserialize<SharingInfo>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}