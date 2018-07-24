using ICD.Connect.Conferencing.Zoom.Responses;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Models
{
	public sealed class ParticipantInfo
	{
		[JsonProperty("user_name")]
		public string UserName { get; private set; }

		[JsonProperty("user_id")]
		public string UserId { get; private set; }

		[JsonProperty("is_host")]
		public bool IsHost { get; private set; }

		[JsonProperty("is_myself")]
		public bool IsMyself { get; private set; }

		[JsonProperty("can_record")]
		public bool CanRecord { get; private set; }

		[JsonProperty("is_recording")]
		public bool IsRecording { get; private set; }

		[JsonProperty("avatar_url")]
		public string AvatarUrl { get; private set; }

		[JsonProperty("local_recording_disabled")]
		public bool LocalRecordingDisabled { get; private set; }

		[JsonProperty("is_video_can_mute_byHost")]
		public bool CanHostMuteVideo { get; private set; }

		[JsonProperty("is_video_can_unmute_byHost")]
		public bool CanHostUnmuteVideo { get; private set; }

		[JsonProperty("isCohost")]
		public bool IsCohost { get; private set; }

		[JsonProperty("user_type")]
		public eUserCallType UserCallType { get; private set; }

		[JsonProperty("audio_status type")]
		public eAudioType AudioType { get; private set; }

		[JsonProperty("audio_status state")]
		public eAudioState AudioState { get; private set; }

		[JsonProperty("video_status has_source")]
		public bool HasVideoSource { get; private set; }

		[JsonProperty("video_status is_receiving")]
		public bool IsReceivingVideo { get; private set; }

		[JsonProperty("video_status is_sending")]
		public bool IsSendingVideo { get; private set; }

		[JsonProperty("camera_status can_i_request_control")]
		public bool CanRequestCameraControl { get; private set; }

		[JsonProperty("camera_status am_i_controlling")]
		public bool AmIControllingCamera { get; private set; }

		[JsonProperty("camera_status can_switch_camera")]
		public bool CanSwitchCamera { get; private set; }

		[JsonProperty("camera_status can_move_camera")]
		public bool CanMoveCamera { get; private set; }

		[JsonProperty("camera_status can_zoom_camera")]
		public bool CanZoomCamera { get; private set; }

		[JsonProperty("event")]
		public eUserChangedEventType Event { get; private set; }
	}
}