using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Call;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class IncomingCallResponseConverter : AbstractZoomRoomResponseConverter<IncomingCallResponse>
	{
		private const string ATTR_INCOMING_CALL_INDICATION = "IncomingCallIndication";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, IncomingCallResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.IncomingCall != null)
			{
				writer.WritePropertyName(ATTR_INCOMING_CALL_INDICATION);
				serializer.Serialize(writer, value.IncomingCall);
			}
		}

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