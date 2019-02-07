using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, true),
	 ZoomRoomApiResponse("ListParticipantsResult", eZoomRoomApiType.zCommand, false)]
	public sealed class ListParticipantsResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("ListParticipantsResult")]
		public List<ParticipantInfo> Participants { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			Participants = jObject["ListParticipantsResult"].Children().Select(o =>
			{
				var participant = new ParticipantInfo();
				participant.LoadFromJObject((JObject) o);
				return participant;
			}).ToList();
		}
	}

	public sealed class SingleParticipantResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("ListParticipantsResult")]
		public ParticipantInfo Participant { get; private set; }

		public override void LoadFromJObject(JObject jObject)
		{
			base.LoadFromJObject(jObject);

			Participant = new ParticipantInfo();
			Participant.LoadFromJObject((JObject)jObject["ListParticipantsResult"]);
		}
	}
}