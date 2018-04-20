using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("CallDisconnect", eZoomRoomApiType.zEvent, false)]
	public sealed class CallDisconnectResponse : AbstractZoomRoomResponse
	{
		[JsonProperty("CallDisconnect")]
		public CallDisconnect Disconnect { get; private set; }
	}

	public sealed class CallDisconnect
	{
		[JsonProperty("success")]
		public eZoomBoolean Success { get; private set; }
	}

	public enum eZoomBoolean
	{
		off,
		on
	}
}