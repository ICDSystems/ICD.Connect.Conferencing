using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, true)]
	public sealed class ListParticipantsCommandResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("ListParticipantsResult")]
		public ParticipantInfo[] ListParticipantsResult { get; set; }
	}

	[ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, false)]
	public sealed class ParticipantUpdateResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("ListParticipantsResult")]
		public ParticipantInfo ListParticipantsResult { get; set; }
	}

	public sealed class ParticipantInfo
	{
		[JsonProperty("user_name")]
		public string UserName { get; set; }

		[JsonProperty("user_id")]
		public string UserId { get; set; }

		[JsonProperty("is_host")]
		public bool IsHost { get; set; }

		[JsonProperty("is_myself")]
		public bool IsMyself { get; set; }

		[JsonProperty("can_record")]
		public bool CanRecord { get; set; }

		[JsonProperty("is_recording")]
		public bool IsRecording { get; set; }

		[JsonProperty("avatar_url")]
		public string AvatarUrl { get; set; }

		[JsonProperty("local_recording_disabled")]
		public bool LocalRecordingDisabled { get; set; }

		[JsonProperty("is_video_can_mute_byHost")]
		public bool CanHostMuteVideo { get; set; }

		[JsonProperty("is_video_can_unmute_byHost")]
		public bool CanHostUnmuteVideo { get; set; }

		[JsonProperty("isCohost")]
		public bool IsCohost { get; set; }

		[JsonProperty("user_type")]
		public eUserCallType UserCallType { get; set; }

		[JsonProperty("audio_status type")]
		public eAudioType AudioType { get; set; }

		[JsonProperty("audio_status state")]
		public eAudioState AudioState { get; set; }

		[JsonProperty("video_status has_source")]
		public bool HasVideoSource { get; set; }

		[JsonProperty("video_status is_receiving")]
		public bool IsReceivingVideo { get; set; }

		[JsonProperty("video_status is_sending")]
		public bool IsSendingVideo { get; set; }

		[JsonProperty("camera_status can_i_request_control")]
		public bool CanRequestCameraControl { get; set; }

		[JsonProperty("camera_status am_i_controlling")]
		public bool AmIControllingCamera { get; set; }

		[JsonProperty("camera_status can_switch_camera")]
		public bool CanSwitchCamera { get; set; }

		[JsonProperty("camera_status can_move_camera")]
		public bool CanMoveCamera { get; set; }

		[JsonProperty("camera_status can_zoom_camera")]
		public bool CanZoomCamera { get; set; }

		[JsonProperty("event")]
		public eUserChangedEventType Event { get; set; }
	}

	public enum eUserCallType
	{
		NORMAL,
		H323,
		CALL_IN
	}

	public enum eAudioType
	{
		/// <summary>
		/// Connected only with video
		/// </summary>
		AUDIO_NONE,
		/// <summary>
		/// Connected over internet
		/// </summary>
		AUDIO_VOIP,
		/// <summary>
		/// Connected via PSTN
		/// </summary>
		AUDIO_TELE
	}

	public enum eAudioState
	{
		AUDIO_UNMUTED,
		AUDIO_MUTED
	}

	public enum eUserChangedEventType
	{
		None,
		ZRCUserChangedEventJoinedMeeting,
		ZRCUserChangedEventLeftMeeting,
		ZRCUserChangedEventUserInfoUpdated,
		ZRCUserChangedEventHostChanged
	}
}