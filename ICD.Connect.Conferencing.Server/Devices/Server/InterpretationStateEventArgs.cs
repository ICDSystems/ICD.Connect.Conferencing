using System;
using ICD.Common.Properties;

namespace ICD.Connect.Conferencing.Server.Devices.Server
{
	public sealed class InterpretationStateEventArgs : EventArgs
	{
		[PublicAPI]
		public int RoomId { get; set; }

		[PublicAPI]
		public ushort BoothId { get; set; }

		[PublicAPI]
		public bool Active { get; set; }

		public InterpretationStateEventArgs(int roomId, ushort boothId, bool active)
		{
			RoomId = roomId;
			BoothId = boothId;
			Active = active;
		}
	}
}