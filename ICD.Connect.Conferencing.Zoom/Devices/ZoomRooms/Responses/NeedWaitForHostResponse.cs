#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	[ZoomRoomApiResponse("NeedWaitForHost", eZoomRoomApiType.zEvent, false),
	 ZoomRoomApiResponse("NeedWaitForHost", eZoomRoomApiType.zEvent, true)]
	[JsonConverter(typeof(NeedWaitForHostResponseConverter))]
	public sealed class NeedWaitForHostResponse : AbstractZoomRoomResponse
	{
		public NeedWaitForHost Response { get; set; }
	}

	[JsonConverter(typeof(NeedWaitForHostConverter))]
	public sealed class NeedWaitForHost
	{
		public bool Wait { get; set; }
	}
}
