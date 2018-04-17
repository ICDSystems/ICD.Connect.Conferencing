using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[AttributeUsage(AttributeTargets.Class)]
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