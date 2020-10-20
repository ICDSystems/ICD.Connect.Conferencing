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

		public InterpretationRoomInfoArgs(int roomId, string roomName)
		{
			RoomId = roomId;
			RoomName = roomName;
		}

		/// <summary>
		/// Empty constructor so S+ can see the class.
		/// </summary>
		public InterpretationRoomInfoArgs()
		{}

	}
}