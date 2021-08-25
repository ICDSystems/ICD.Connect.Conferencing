#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters
{
	public sealed class CameraControlNotificationResponseConverter : AbstractZoomRoomResponseConverter<CameraControlNotificationResponse>
	{
		private const string ATTR_CAMERA_CONTROL_NOTIFICATION = "CameraControlNotification";

		protected override void WriteProperties(JsonWriter writer, CameraControlNotificationResponse value,
		                                        JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.CameraControlNotification != null)
			{
				writer.WritePropertyName(ATTR_CAMERA_CONTROL_NOTIFICATION);
				serializer.Serialize(writer, value.CameraControlNotification);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, CameraControlNotificationResponse instance,
		                                     JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_CAMERA_CONTROL_NOTIFICATION:
					instance.CameraControlNotification = serializer.Deserialize<CameraControlNotification>(reader);
					break;
				
				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}

	public sealed class CameraControlNotificationConverter : AbstractGenericJsonConverter<CameraControlNotification>
	{
		private const string ATTR_STATE = "state";
		private const string ATTR_USER_ID = "user_id";
		private const string ATTR_USER_NAME = "user_name";

		protected override void WriteProperties(JsonWriter writer, CameraControlNotification value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.State != eCameraControlNotificationState.None)
			{
				writer.WritePropertyName(ATTR_STATE);
				serializer.Serialize(writer, value.State);
			}

			if (value.UserId != null)
			{
				writer.WritePropertyName(ATTR_USER_ID);
				serializer.Serialize(writer, value.UserId);
			}

			if (value.UserName != null)
			{
				writer.WritePropertyName(ATTR_USER_NAME);
				serializer.Serialize(writer, value.UserName);
			}
		}

		protected override void ReadProperty(string property, JsonReader reader, CameraControlNotification instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_STATE:
					instance.State = reader.GetValueAsEnum<eCameraControlNotificationState>();
					break;
				case ATTR_USER_ID:
					instance.UserId = reader.GetValueAsString();
					break;
				case ATTR_USER_NAME:
					instance.UserName = reader.GetValueAsString();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}