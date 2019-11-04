using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class ListParticipantsResponseConverter : AbstractZoomRoomResponseConverter<ListParticipantsResponse>
	{
		private const string ATTR_LIST_PARTICIPANTS_RESULT = "ListParticipantsResult";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, ListParticipantsResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Participants != null && value.Participants.Count > 0)
			{
				writer.WritePropertyName(ATTR_LIST_PARTICIPANTS_RESULT);
				serializer.SerializeArray(writer, value.Participants);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, ListParticipantsResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_LIST_PARTICIPANTS_RESULT:
					// Sometimes a single participant comes back as a single object
					if (reader.TokenType == JsonToken.StartArray)
						instance.Participants = serializer.DeserializeArray<ParticipantInfo>(reader).ToList();
					else
					{
						ParticipantInfo participant = serializer.Deserialize<ParticipantInfo>(reader);
						instance.Participants = new List<ParticipantInfo> {participant};
					}
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
