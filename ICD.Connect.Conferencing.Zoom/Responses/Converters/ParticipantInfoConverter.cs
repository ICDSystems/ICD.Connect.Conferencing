using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses.Converters
{
	public sealed class ParticipantInfoConverter : AbstractGenericJsonConverter<ParticipantInfo>
	{
		private const string ATTR_USER_NAME = "user_name";
		private const string ATTR_USER_ID = "user_id";
		private const string ATTR_IS_HOST = "is_host";
		private const string ATTR_IS_MYSELF = "is_myself";
		private const string ATTR_CAN_RECORD = "can_record";
		private const string ATTR_IS_RECORDING = "is_recording";
		private const string ATTR_AVATAR_URL = "avatar_url";
		private const string ATTR_LOCAL_RECORDING_DISABLED = "local_recording_disabled";
		private const string ATTR_IS_VIDEO_CAN_MUTE_BY_HOST = "is_video_can_mute_byHost";
		private const string ATTR_IS_VIDEO_CAN_UNMUTE_BY_HOST = "is_video_can_unmute_byHost";
		private const string ATTR_IS_COHOST = "isCohost";
		private const string ATTR_USER_TYPE = "user_type";
		private const string ATTR_AUDIO_STATUS_TYPE = "audio_status type";
		private const string ATTR_AUDIO_STATUS_STATE = "audio_status state";
		private const string ATTR_VIDEO_STATUS_HAS_SOURCE = "video_status has_source";
		private const string ATTR_VIDEO_STATUS_IS_RECEIVING = "video_status is_receiving";
		private const string ATTR_VIDEO_STATUS_IS_SENDING = "video_status is_sending";
		private const string ATTR_CAMERA_STATUS_CAN_I_REQUEST_CONTROL = "camera_status can_i_request_control";
		private const string ATTR_CAMERA_STATUS_AM_I_CONTROLLING = "camera_status am_i_controlling";
		private const string ATTR_CAMERA_STATUS_CAN_SWITCH_CAMERA = "camera_status can_switch_camera";
		private const string ATTR_CAMERA_STATUS_CAN_MOVE_CAMERA = "camera_status can_move_camera";
		private const string ATTR_CAMERA_STATUS_CAN_ZOOM_CAMERA = "camera_status can_zoom_camera";
		private const string ATTR_EVENT = "event";

		/// <summary>
		/// Override to write properties to the writer.
		/// </summary>
		/// <param name="writer"></param>
		/// <param name="value"></param>
		/// <param name="serializer"></param>
		protected override void WriteProperties(JsonWriter writer, ParticipantInfo value, JsonSerializer serializer)
		{
			base.WriteProperties(writer, value, serializer);

			if (value.UserName != null)
				writer.WriteProperty(ATTR_USER_NAME, value.UserName);

			if (value.UserId != null)
				writer.WriteProperty(ATTR_USER_ID, value.UserId);

			if (value.IsHost)
				writer.WriteProperty(ATTR_IS_HOST, value.IsHost);

			if (value.IsMyself)
				writer.WriteProperty(ATTR_IS_MYSELF, value.IsMyself);

			if (value.AvatarUrl != null)
				writer.WriteProperty(ATTR_AVATAR_URL, value.AvatarUrl);

			if (value.IsCohost)
				writer.WriteProperty(ATTR_IS_COHOST, value.IsCohost);

			if (value.AudioState != default(eAudioState))
				writer.WriteProperty(ATTR_AUDIO_STATUS_STATE, value.AudioState);

			if (value.IsSendingVideo)
				writer.WriteProperty(ATTR_VIDEO_STATUS_IS_SENDING, value.IsSendingVideo);

			if (value.Event != default(eUserChangedEventType))
				writer.WriteProperty(ATTR_EVENT, value.Event);

			if (value.CanRecord)
				writer.WriteProperty(ATTR_CAN_RECORD, value.CanRecord);

			if (value.IsRecording)
				writer.WriteProperty(ATTR_IS_RECORDING, value.IsRecording);
		}

		protected override void ReadProperty(string property, JsonReader reader, ParticipantInfo instance, JsonSerializer serializer)
		{
			switch (property)
			{
				case ATTR_USER_NAME:
					instance.UserName = reader.GetValueAsString();
					break;
				case ATTR_USER_ID:
					instance.UserId = reader.GetValueAsString();
					break;
				case ATTR_IS_HOST:
					instance.IsHost = reader.GetValueAsBool();
					break;
				case ATTR_IS_MYSELF:
					instance.IsMyself = reader.GetValueAsBool();
					break;
				case ATTR_AVATAR_URL:
					instance.AvatarUrl = reader.GetValueAsString();
					break;
				case ATTR_IS_COHOST:
					instance.IsCohost = reader.GetValueAsBool();
					break;
				case ATTR_AUDIO_STATUS_STATE:
					instance.AudioState = reader.GetValueAsEnum<eAudioState>();
					break;
				case ATTR_VIDEO_STATUS_IS_SENDING:
					instance.IsSendingVideo = reader.GetValueAsBool();
					break;
				case ATTR_EVENT:
					instance.Event = reader.GetValueAsEnum<eUserChangedEventType>();
					break;
				case ATTR_CAN_RECORD:
					instance.CanRecord = reader.GetValueAsBool();
					break;
				case ATTR_IS_RECORDING:
					instance.IsRecording = reader.GetValueAsBool();
					break;

				default:
					base.ReadProperty(property, reader, instance, serializer);
					break;
			}
		}
	}
}
