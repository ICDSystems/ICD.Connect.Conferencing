#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("PhoneCallStatus", eZoomRoomApiType.zEvent, false),
	 ZoomRoomApiResponse("PhoneCallStatus", eZoomRoomApiType.zEvent, true)]
	[JsonConverter(typeof(PhoneCallStatusResponseConverter))]
	public sealed class PhoneCallStatusResponse : AbstractZoomRoomResponse
	{
		[CanBeNull]
		public PhoneCallStatus PhoneCallStatus { get; set; }
	}

	[JsonConverter(typeof(PhoneCallStatusConverter))]
	public sealed class PhoneCallStatus
	{
		public string CallId { get; set; }

		public bool IsIncomingCall { get; set; }

		public string PeerDisplayName { get; set; }

		public string PeerNumber { get; set; }

		public string PeerUri { get; set; }
		
		public eZoomPhoneCallStatus Status { get; set; }
	}

	[JsonConverter(typeof(ZoomPhoneCallStatusConverter))]
	public enum eZoomPhoneCallStatus
	{
		None = 0,
		Ringing = 1,
		Init = 2,
		InCall = 3,
		Incoming = 4,
		NotFound = 5,
		CallOutFailed = 6
	}

	[ZoomRoomApiResponse("PhoneCallTerminated", eZoomRoomApiType.zEvent, false),
	 ZoomRoomApiResponse("PhoneCallTerminated", eZoomRoomApiType.zEvent, true)]
	[JsonConverter(typeof(PhoneCallTerminatedResponseConverter))]
	public sealed class PhoneCallTerminatedResponse : AbstractZoomRoomResponse
	{
		public PhoneCallTerminated PhoneCallTerminated { get; set; }
	}

	[JsonConverter(typeof(PhoneCallTerminatedConverter))]
	public sealed class PhoneCallTerminated
	{
		public string CallId { get; set; }

		public bool IsIncomingCall { get; set; }
		public string PeerDisplayName { get; set; }

		public string PeerNumber { get; set; }

		public string PeerUri { get; set; }

		public eZoomPhoneCallTerminatedReason Reason { get; set; }
	}

	[JsonConverter(typeof(ZoomPhoneCallTerminatedReasonConverter))]
	public enum eZoomPhoneCallTerminatedReason
	{
		None = 0,
		ByLocal = 1,
		ByRemote = 2,
		ByInitAudioDeviceFailed = 3
	}
}
