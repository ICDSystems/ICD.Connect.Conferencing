using System;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.TraditionalCall
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

		public void UpdateStatusInfo([NotNull] PhoneCallStatus status)
		{
			if (status == null)
				throw new ArgumentNullException("status");

			CallId = status.CallId;
			PeerDisplayName = status.PeerDisplayName;
			PeerNumber = status.PeerNumber;
			PeerUri = status.PeerUri;
			IsIncomingCall = status.IsIncomingCall;
			Status = status.Status;
		}

		public void UpdateTerminateInfo([NotNull] PhoneCallTerminated terminated)
		{
			if (terminated == null)
				throw new ArgumentNullException("terminated");

			CallId = terminated.CallId;
			PeerDisplayName = terminated.PeerDisplayName;
			PeerNumber = terminated.PeerNumber;
			PeerUri = terminated.PeerUri;
			IsIncomingCall = terminated.IsIncomingCall;
			Reason = terminated.Reason;
		}
	}
}
