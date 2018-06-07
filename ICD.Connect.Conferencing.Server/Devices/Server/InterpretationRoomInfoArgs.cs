using System;
using ICD.Common.Properties;

namespace ICD.Connect.Conferencing.Server.Devices.Server
{
	public sealed class InterpretationRoomInfoArgs : EventArgs
	{
		[PublicAPI]
		public int RoomId { get; set; }

		[PublicAPI]
		public string RoomName { get; set; }

		[PublicAPI]
		public string RoomPrefix { get; set; }

		public InterpretationRoomInfoArgs(int roomId, string roomName, string roomPrefix)
		{
			RoomId = roomId;
			RoomName = roomName;
			RoomPrefix = roomPrefix;
		}

	}
}