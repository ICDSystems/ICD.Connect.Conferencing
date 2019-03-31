using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	public enum eZoomRoomResponseState
	{
		OK,
		Error
	}

	[JsonConverter(typeof(ZoomRoomResponseStatusConverter))]
	public sealed class ZoomRoomResponseStatus
	{
		public string Message { get; set; }

		public eZoomRoomResponseState State { get; set; }
	}
}