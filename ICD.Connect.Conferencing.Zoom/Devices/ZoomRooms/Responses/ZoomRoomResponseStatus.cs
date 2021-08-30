#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
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