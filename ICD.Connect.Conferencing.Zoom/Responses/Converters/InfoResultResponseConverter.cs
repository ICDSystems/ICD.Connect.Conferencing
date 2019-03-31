using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class InfoResultResponseConverter : AbstractGenericJsonConverter<InfoResultResponse>
	{
		private const string ATTR_INFO_RESULT = "InfoResult";

		protected override void ReadProperty(string property, JsonReader reader, InfoResultResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_INFO_RESULT:
					instance.InfoResult = serializer.Deserialize<CallInfo>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}