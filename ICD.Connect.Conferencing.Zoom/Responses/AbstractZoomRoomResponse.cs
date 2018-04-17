using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	public abstract class AbstractZoomRoomResponse
	{
		/// <summary>
		/// Property key of where the actual response data is stored
		/// </summary>
		public string TopKey { get; set; }

		/// <summary>
		/// The type of response
		/// </summary>
		public eZoomRoomApiType Type { get; set; }

		/// <summary>
		/// Whether the command succeeded or not
		/// </summary>
		public ZoomRoomResponseStatus Status { get; set; }

		/// <summary>
		/// Whether or not this is a synchronous response to a command (true)
		/// or asynchronous status update (false)
		/// </summary>
		public bool Sync { get; set; }
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
		public string Message { get; set; }
		public string State { get; set; }
	}
}