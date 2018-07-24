using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Models;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	/// <summary>
	/// Contains call info, received either as an event, status update, or command
	/// </summary>
	[ZoomRoomApiResponse("InfoResult", eZoomRoomApiType.zCommand, true)]
	public sealed class InfoResultResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("InfoResult")]
		public CallInfo InfoResult { get; private set; }
	}
}