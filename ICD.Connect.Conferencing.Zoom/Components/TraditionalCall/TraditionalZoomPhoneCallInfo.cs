using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.TraditionalCall
{
	public sealed class TraditionalZoomPhoneCallInfo
	{
		public string CallId { get; set; }

		public string PeerDisplayName { get; set; }

		public string PeerNumber { get; set; }

		public string PeerUri { get; set; }

		public bool IsIncomingCall { get; set; }

		public eZoomPhoneCallStatus Status { get; set; }

		public eZoomPhoneCallTerminatedReason Reason { get; set; }

		public void UpdateStatusInfo(PhoneCallStatus status)
		{
			CallId = status.CallId;
			PeerDisplayName = status.PeerDisplayName;
			PeerNumber = status.PeerNumber;
			PeerUri = status.PeerUri;
			IsIncomingCall = status.IsIncomingCall;
			Status = status.Status;
		}

		public void UpdateTerminateInfo(PhoneCallTerminated terminated)
		{
			CallId = terminated.CallId;
			PeerDisplayName = terminated.PeerDisplayName;
			PeerNumber = terminated.PeerNumber;
			PeerUri = terminated.PeerUri;
			IsIncomingCall = terminated.IsIncomingCall;
			Reason = terminated.Reason;
		}
	}
}
