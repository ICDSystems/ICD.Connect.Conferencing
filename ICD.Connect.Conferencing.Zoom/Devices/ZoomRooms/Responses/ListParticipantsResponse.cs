#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System.Collections.Generic;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Call;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, true),
	 ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, false)]
	[JsonConverter(typeof(ListParticipantsResponseConverter))]
	public sealed class ListParticipantsResponse : AbstractZoomRoomResponse
	{
		public List<ParticipantInfo> Participants { get; set; }
	}
}