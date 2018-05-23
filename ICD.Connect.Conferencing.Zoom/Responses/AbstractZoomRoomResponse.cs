using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	// do not put a [JsonConverter(typeof(ZoomRoomResponseConverter)] attribute here, will infinite loop
	public abstract class AbstractZoomRoomResponse
	{
		/// <summary>
		/// Property key of where the actual response data is stored
		/// </summary>
		[JsonProperty("topKey")]
		public string TopKey { get; private set; }

		/// <summary>
		/// The type of response
		/// </summary>
		[JsonProperty("type")]
		public eZoomRoomApiType Type { get; private set; }

		/// <summary>
		/// Whether the command succeeded or not
		/// </summary>
		[JsonProperty("Status")]
		public ZoomRoomResponseStatus Status { get; private set; }

		/// <summary>
		/// Whether or not this is a synchronous response to a command (true)
		/// or asynchronous status update (false)
		/// </summary>
		[JsonProperty("Sync")]
		public bool Sync { get; private set; }
	}

	public enum eZoomRoomApiType
	{
		zCommand,
		zConfiguration,
		zStatus,
		zEvent
	}

	public class ZoomRoomResponseStatus
	{
		[JsonProperty("message")]
		public string Message { get; private set; }
		[JsonProperty("state")]
		public eZoomRoomResponseState State { get; private set; }
	}

	public enum eZoomRoomResponseState
	{
		OK,
		Error
	}
}