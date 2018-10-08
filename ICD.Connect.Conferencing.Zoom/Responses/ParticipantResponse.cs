using System.Collections.Generic;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, true),
	 ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, false)]
	public sealed class ListParticipantsResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("ListParticipantsResult")]
		public List<ParticipantInfo> Participants { get; private set; }
	}

	public sealed class SingleParticipantResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("ListParticipantsResult")]
		public ParticipantInfo Participant { get; private set; }
	}
}