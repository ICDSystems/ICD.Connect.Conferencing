using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	[JsonConverter(typeof(ParticipantInfoConverter))]
	public sealed class ParticipantInfo
	{
		public string UserName { get; set; }

		public string UserId { get; set; }

		public bool IsHost { get; set; }

		public bool IsMyself { get; set; }

		public bool CanRecord { get; set; }

		public bool IsRecording { get; set; }

		public string AvatarUrl { get; set; }

		public bool LocalRecordingDisabled { get; set; }

		public bool CanHostMuteVideo { get; set; }

		public bool CanHostUnmuteVideo { get; set; }

		public bool IsCohost { get; set; }

		public eUserCallType UserCallType { get; set; }

		public eAudioType AudioType { get; set; }

		public eAudioState AudioState { get; set; }

		public bool HasVideoSource { get; set; }

		public bool IsReceivingVideo { get; set; }

		public bool IsSendingVideo { get; set; }

		public bool CanRequestCameraControl { get; set; }

		public bool AmIControllingCamera { get; set; }

		public bool CanSwitchCamera { get; set; }

		public bool CanMoveCamera { get; set; }

		public bool CanZoomCamera { get; set; }

		public eUserChangedEventType Event { get; set; }
	}
}