using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class ListParticipantsResponseConverter : AbstractGenericJsonConverter<ListParticipantsResponse>
	{
		private const string ATTR_LIST_PARTICIPANTS_RESULT = "ListParticipantsResult";

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
