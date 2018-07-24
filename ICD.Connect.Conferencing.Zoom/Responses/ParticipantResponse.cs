using ICD.Connect.Conferencing.Zoom.Models;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, true)]
	public sealed class ListParticipantsCommandResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("ListParticipantsResult")]
		public ParticipantInfo[] ParticipantList { get; private set; }
	}

	[ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, false)]
	public sealed class ParticipantUpdateResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("ListParticipantsResult")]
		public ParticipantInfo Participant { get; private set; }
	}
}