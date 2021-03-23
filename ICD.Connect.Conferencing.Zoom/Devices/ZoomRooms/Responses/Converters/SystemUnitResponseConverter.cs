using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class SystemUnitResponseConverter : AbstractZoomRoomResponseConverter<SystemUnitResponse>
	{
		private const string ATTR_SYSTEM_UNIT = "SystemUnit";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, SystemUnitResponse value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.SystemInfo != null)
			{
				writer.WritePropertyName(ATTR_SYSTEM_UNIT);
				serializer.Serialize(writer, value.SystemInfo);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, SystemUnitResponse instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_SYSTEM_UNIT:
					instance.SystemInfo = serializer.Deserialize<SystemInfo>(reader);
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class SystemInfoConverter : AbstractGenericJsonConverter<SystemInfo>
	{
		private const string ATTR_EMAIL = "email";
		private const string ATTR_LOGIN_TYPE = "login_type";
		private const string ATTR_MEETING_NUMBER = "meeting_number";
		private const string ATTR_PLATFORM = "platform";
		private const string ATTR_ROOM_INFO = "room_info";
		private const string ATTR_ROOM_VERSION = "room_version";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, SystemInfo value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.Email != null)
				writer.WriteProperty(ATTR_EMAIL, value.Email);

			if (value.LoginType != default(eLoginType))
				writer.WriteProperty(ATTR_LOGIN_TYPE, value.LoginType);

			if (value.MeetingNumber != null)
				writer.WriteProperty(ATTR_MEETING_NUMBER, value.MeetingNumber);

			if (value.Platform != null)
				writer.WriteProperty(ATTR_PLATFORM, value.Platform);

			if (value.RoomInfo != null)
			{
				writer.WritePropertyName(ATTR_ROOM_INFO);
				serializer.Serialize(writer, value.RoomInfo);
			}

			if (value.RoomVersion != null)
				writer.WriteProperty(ATTR_ROOM_VERSION, value.RoomVersion);
		}

		protected override void ReadProperty(string property, JsonReader reader, SystemInfo instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_EMAIL:
					instance.Email = reader.GetValueAsString();
					break;
				case ATTR_LOGIN_TYPE:
					instance.LoginType = reader.GetValueAsEnum<eLoginType>();
					break;
				case ATTR_MEETING_NUMBER:
					instance.MeetingNumber = reader.GetValueAsString();
					break;
				case ATTR_PLATFORM:
					instance.Platform = reader.GetValueAsString();
					break;
				case ATTR_ROOM_INFO:
					instance.RoomInfo = serializer.Deserialize<RoomInfo>(reader);
					break;
				case ATTR_ROOM_VERSION:
					instance.RoomVersion = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class RoomInfoConverter : AbstractGenericJsonConverter<RoomInfo>
	{
		private const string ATTR_ROOM_NAME = "room_name";
		private const string ATTR_IS_AUTO_ANSWER_ENABLED = "is_auto_answer_enabled";
		private const string ATTR_IS_AUTO_ANSWER_SELECTED = "is_auto_answer_selected";
		private const string ATTR_ACCOUNT_EMAIL = "account_email";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, RoomInfo value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.RoomName != null)
				writer.WriteProperty(ATTR_ROOM_NAME, value.RoomName);

			if (value.IsAutoAnswerEnabled)
				writer.WriteProperty(ATTR_IS_AUTO_ANSWER_ENABLED, value.IsAutoAnswerEnabled);

			if (value.IsAutoAnswerSelected)
				writer.WriteProperty(ATTR_IS_AUTO_ANSWER_SELECTED, value.IsAutoAnswerSelected);

			if (value.AccountEmail != null)
				writer.WriteProperty(ATTR_ACCOUNT_EMAIL, value.AccountEmail);
		}

		protected override void ReadProperty(string property, JsonReader reader, RoomInfo instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_ROOM_NAME:
					instance.RoomName = reader.GetValueAsString();
					break;
				case ATTR_IS_AUTO_ANSWER_ENABLED:
					instance.IsAutoAnswerEnabled = reader.GetValueAsBool();
					break;
				case ATTR_IS_AUTO_ANSWER_SELECTED:
					instance.IsAutoAnswerSelected = reader.GetValueAsBool();
					break;
				case ATTR_ACCOUNT_EMAIL:
					instance.AccountEmail = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}