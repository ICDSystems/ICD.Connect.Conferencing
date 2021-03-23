using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Directory;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("Phonebook", eZoomRoomApiType.zEvent, false)]
	[JsonConverter(typeof(PhonebookContactUpdatedResponseConverter))]
	public sealed class PhonebookContactUpdatedResponse : AbstractZoomRoomResponse
	{
		public PhonebookUpdatedContact Data { get; set; }
	}

	[ZoomRoomApiResponse("PhonebookListResult", eZoomRoomApiType.zCommand, true)]
	[JsonConverter(typeof(PhonebookListCommandResponseConverter))]
	public sealed class PhonebookListCommandResponse : AbstractZoomRoomResponse
	{
		public PhonebookListResult PhonebookListResult { get; set; }
	}

	[JsonConverter(typeof(PhonebookListResultConverter))]
	public sealed class PhonebookListResult
	{
		public ZoomContact[] Contacts { get; set; }

		public int Limit { get; set; }

		public int Offset { get; set; }
	}

	[JsonConverter(typeof(PhonebookUpdatedContactConverter))]
	public sealed class PhonebookUpdatedContact
	{
		public ZoomContact Contact { get; set; }
	}
}