using System;
using ICD.Common.Properties;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Attributes
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	[MeansImplicitUse]
	public class ZoomRoomApiResponseAttribute : Attribute
	{
		public string ResponseKey { get; private set; }

		public eZoomRoomApiType CommandType { get; private set; }

		public bool Synchronous { get; private set; }

		public ZoomRoomApiResponseAttribute(string responseKey, eZoomRoomApiType commandType, bool synchronous)
		{
			ResponseKey = responseKey;
			CommandType = commandType;
			Synchronous = synchronous;
		}
	}
}