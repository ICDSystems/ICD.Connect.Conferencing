using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Directory;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class PhonebookContactUpdatedResponseConverter : AbstractZoomRoomResponseConverter<PhonebookContactUpdatedResponse>
	{
		private const string ATTR_PHONEBOOK = "Phonebook";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, PhonebookContactUpdatedResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Data != null)
			{
				writer.WritePropertyName(ATTR_PHONEBOOK);
				serializer.Serialize(writer, value.Data);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, PhonebookContactUpdatedResponse instance,
			JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_PHONEBOOK:
					instance.Data = serializer.Deserialize<PhonebookUpdatedContact>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class PhonebookListCommandResponseConverter : AbstractZoomRoomResponseConverter<PhonebookListCommandResponse>
	{
		private const string ATTR_PHONEBOOK_LIST_RESULT = "PhonebookListResult";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, PhonebookListCommandResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.PhonebookListResult != null)
			{
				writer.WritePropertyName(ATTR_PHONEBOOK_LIST_RESULT);
				serializer.Serialize(writer, value.PhonebookListResult);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, PhonebookListCommandResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_PHONEBOOK_LIST_RESULT:
					instance.PhonebookListResult = serializer.Deserialize<PhonebookListResult>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class PhonebookListResultConverter : AbstractGenericJsonConverter<PhonebookListResult>
	{
		private const string ATTR_CONTACTS = "Contacts";
		private const string ATTR_LIMIT = "Limit";
		private const string ATTR_OFFSET = "Offset";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, PhonebookListResult value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Contacts != null && value.Contacts.Length != 0)
			{
				writer.WritePropertyName(ATTR_CONTACTS);
				serializer.SerializeArray(writer, value.Contacts);
			}

			if (value.Limit != 0)
				writer.WriteProperty(ATTR_LIMIT, value.Limit);

			if (value.Offset != 0)
				writer.WriteProperty(ATTR_OFFSET, value.Offset);
		}

		protected override void ReadProperty(string property, JsonReader reader, PhonebookListResult instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CONTACTS:
					instance.Contacts = serializer.DeserializeArray<ZoomContact>(reader).ToArray();
					break;

				case ATTR_LIMIT:
					instance.Limit = reader.GetValueAsInt();
					break;

				case ATTR_OFFSET:
					instance.Offset = reader.GetValueAsInt();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class PhonebookUpdatedContactConverter : AbstractGenericJsonConverter<PhonebookUpdatedContact>
	{
		private const string ATTR_UPDATED_CONTACT = "Updated Contact";
		private const string ATTR_ADDED_CONTACT = "Added Contact";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, PhonebookUpdatedContact value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Contact != null)
			{
				// TODO - How should these be handled?
				writer.WritePropertyName(ATTR_UPDATED_CONTACT);
				serializer.Serialize(writer, value.Contact);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, PhonebookUpdatedContact instance, JsonSerializer serializer)
		{
			switch (property)
			{
				// TODO - How should these be handled?
				case ATTR_UPDATED_CONTACT:
				case ATTR_ADDED_CONTACT:
					instance.Contact = serializer.Deserialize<ZoomContact>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
