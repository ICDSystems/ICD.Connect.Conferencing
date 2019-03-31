using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class IncomingCallResponseConverter : AbstractGenericJsonConverter<IncomingCallResponse>
	{
		private const string ATTR_INCOMING_CALL_INDICATION = "IncomingCallIndication";

		protected override void ReadProperty(string property, JsonReader reader, IncomingCallResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_INCOMING_CALL_INDICATION:
					instance.IncomingCall = serializer.Deserialize<IncomingCall>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}