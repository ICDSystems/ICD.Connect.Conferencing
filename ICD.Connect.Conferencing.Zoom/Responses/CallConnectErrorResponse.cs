using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("CallConnectError", eZoomRoomApiType.zEvent, false)]
	public sealed class CallConnectErrorResponse
	{
		[JsonProperty("CallConnectError")]
		public CallConnectError Error { get; private set; }
	}

	public sealed class CallConnectError
	{
		[JsonProperty("error_code")]
		public int ErrorCode { get; private set; }

		[JsonProperty("error_message")]
		public string ErrorMessage { get; private set; }
	}
}