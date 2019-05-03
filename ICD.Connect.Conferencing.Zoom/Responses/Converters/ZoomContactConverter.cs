using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Directory;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class ZoomContactConverter : AbstractGenericJsonConverter<ZoomContact>
	{
		private const string ATTR_JOIN_ID = "jid";
		private const string ATTR_SCREEN_NAME = "screenName";
		private const string ATTR_FIRST_NAME = "firstName";
		private const string ATTR_LAST_NAME = "lastName";
		private const string ATTR_PHONE_NUMBER = "phoneNumber";
		private const string ATTR_EMAIL = "email";
		private const string ATTR_AVATAR_URL = "avatarURL";
		private const string ATTR_PRESENCE = "presence";
		private const string ATTR_IS_ZOOM_ROOM = "isZoomRoom";
		private const string ATTR_INDEX = "index";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, ZoomContact value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.JoinId != null)
				writer.WriteProperty(ATTR_JOIN_ID, value.JoinId);

			if (value.ScreenName != null)
				writer.WriteProperty(ATTR_SCREEN_NAME, value.ScreenName);

			if (value.FirstName != null)
				writer.WriteProperty(ATTR_FIRST_NAME, value.FirstName);

			if (value.LastName != null)
				writer.WriteProperty(ATTR_LAST_NAME, value.LastName);

			if (value.PhoneNumber != null)
				writer.WriteProperty(ATTR_PHONE_NUMBER, value.PhoneNumber);

			if (value.Email != null)
				writer.WriteProperty(ATTR_EMAIL, value.Email);

			if (value.AvatarUrl != null)
				writer.WriteProperty(ATTR_AVATAR_URL, value.AvatarUrl);

			if (value.Presence != default(eContactPresence))
				writer.WriteProperty(ATTR_PRESENCE, value.Presence);

			if (value.IsZoomRoom)
				writer.WriteProperty(ATTR_IS_ZOOM_ROOM, value.IsZoomRoom);

			if (value.Index != null)
				writer.WriteProperty(ATTR_INDEX, value.Index.Value);
		}

		protected override void ReadProperty(string property, JsonReader reader, ZoomContact instance,
			JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_JOIN_ID:
					instance.JoinId = reader.GetValueAsString();
					break;
				case ATTR_SCREEN_NAME:
					instance.ScreenName = reader.GetValueAsString();
					break;
				case ATTR_FIRST_NAME:
					instance.FirstName = reader.GetValueAsString();
					break;
				case ATTR_LAST_NAME:
					instance.LastName = reader.GetValueAsString();
					break;
				case ATTR_PHONE_NUMBER:
					instance.PhoneNumber = reader.GetValueAsString();
					break;
				case ATTR_EMAIL:
					instance.Email = reader.GetValueAsString();
					break;
				case ATTR_AVATAR_URL:
					instance.AvatarUrl = reader.GetValueAsString();
					break;
				case ATTR_PRESENCE:
					instance.Presence = reader.GetValueAsEnum<eContactPresence>();
					break;
				case ATTR_IS_ZOOM_ROOM:
					instance.IsZoomRoom = reader.GetValueAsBool();
					break;
				case ATTR_INDEX:
					instance.Index = reader.TokenType == JsonToken.Null ? (int?)null : reader.GetValueAsInt();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
