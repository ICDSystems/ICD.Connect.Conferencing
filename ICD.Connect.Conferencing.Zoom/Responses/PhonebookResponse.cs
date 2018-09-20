using ICD.Connect.Conferencing.Zoom.Components.Directory;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Phonebook", eZoomRoomApiType.zEvent, false)]
	public sealed class PhonebookContactUpdatedResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("Phonebook")]
		public PhonebookUpdatedContact Data { get; private set; }
	}

	[ZoomRoomApiResponse("PhonebookListResult", eZoomRoomApiType.zCommand, true)]
	public sealed class PhonebookListCommandResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("PhonebookListResult")]
		public PhonebookListResult PhonebookListResult { get; private set; }
	}

	public sealed class PhonebookListResult
	{
		[JsonProperty("Contacts")]
		public ZoomContact[] Contacts { get; private set; }

		[JsonProperty("Limit")]
		public int Limit { get; private set; }

		[JsonProperty("Offset")]
		public int Offset { get; private set; }
	}

	public sealed class PhonebookUpdatedContact
	{
		[JsonProperty("Updated Contact")]
		public ZoomContact Contact { get; private set; }
	}
}