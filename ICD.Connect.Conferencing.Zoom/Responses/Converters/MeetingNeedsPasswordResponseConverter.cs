using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class MeetingNeedsPasswordResponseConverter : AbstractGenericJsonConverter<MeetingNeedsPasswordResponse>
	{
		private const string ATTR_MEETING_NEEDS_PASSWORD = "MeetingNeedsPassword";

		protected override void WriteProperties(JsonWriter writer, MeetingNeedsPasswordResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.MeetingNeedsPassword != null)
			{
				writer.WritePropertyName(ATTR_MEETING_NEEDS_PASSWORD);
				serializer.Serialize(writer, value.MeetingNeedsPassword);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, MeetingNeedsPasswordResponse instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_MEETING_NEEDS_PASSWORD:
					instance.MeetingNeedsPassword = serializer.Deserialize<MeetingNeedsPasswordEvent>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class MeetingNeedsPasswordEventConverter : AbstractGenericJsonConverter<MeetingNeedsPasswordEvent>
	{
		private const string ATTR_NEEDS_PASSWORD = "needsPassword";
		private const string ATTR_WRONG_AND_RETRY = "wrongAndRetry";

		protected override void WriteProperties(JsonWriter writer, MeetingNeedsPasswordEvent value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.NeedsPassword)
				writer.WriteProperty(ATTR_NEEDS_PASSWORD, value.NeedsPassword);

			if (value.WrongAndRetry)
				writer.WriteProperty(ATTR_WRONG_AND_RETRY, value.WrongAndRetry);
		}

		protected override void ReadProperty(string property, JsonReader reader, MeetingNeedsPasswordEvent instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_NEEDS_PASSWORD:
					instance.NeedsPassword = reader.GetValueAsBool();
					break;
				case ATTR_WRONG_AND_RETRY:
					instance.WrongAndRetry = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
