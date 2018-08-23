using ICD.Connect.Conferencing.Zoom.Components.Call;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	/// <summary>
	/// Contains call info, received either as an event, status update, or command
	/// </summary>
	[ZoomRoomApiResponse("InfoResult", eZoomRoomApiType.zCommand, true),
	 ZoomRoomApiResponse("InfoResult", eZoomRoomApiType.zCommand, false)]
	public sealed class InfoResultResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("InfoResult")]
		public CallInfo InfoResult { get; private set; }
	}
}