using System.Collections.Generic;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, true),
	 ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, false)]
	[JsonConverter(typeof(ListParticipantsResponseConverter))]
	public sealed class ListParticipantsResponse : AbstractZoomRoomResponse
	{
		public List<ParticipantInfo> Participants { get; set; }
	}
}