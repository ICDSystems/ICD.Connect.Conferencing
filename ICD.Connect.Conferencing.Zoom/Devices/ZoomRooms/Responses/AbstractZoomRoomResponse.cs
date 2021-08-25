#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using System;
using ICD.Common.Properties;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	public enum eZoomRoomApiType
	{
		zCommand,
		zConfiguration,
		zStatus,
		zEvent,
		zError
	}

	public abstract class AbstractZoomRoomResponse
	{
		private static readonly JsonSerializerSettings s_Settings =
			new JsonSerializerSettings
		{
#if !SIMPLSHARP
			DateParseHandling = DateParseHandling.None
#endif
		};

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

		[CanBeNull]
		public static AbstractZoomRoomResponse DeserializeResponse(string data, out AttributeKey key)
		{
			if (!AttributeKey.TryParse(data, out key))
				return null;

			// Find concrete type that matches the json values
			Type responseType = key.GetResponseType();
			return responseType == null
				? null
				: JsonConvert.DeserializeObject(data, responseType, s_Settings) as AbstractZoomRoomResponse;
		}
	}
}
