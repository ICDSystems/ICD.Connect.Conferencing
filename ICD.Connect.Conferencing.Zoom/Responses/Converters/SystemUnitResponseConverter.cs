using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class SystemUnitResponseConverter : AbstractGenericJsonConverter<SystemUnitResponse>
	{
		private const string ATTR_SYSTEM_UNIT = "SystemUnit";

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