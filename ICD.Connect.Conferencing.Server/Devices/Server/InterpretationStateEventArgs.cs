using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;

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

		[PublicAPI("S+")]
		public ushort ActiveSPlus { get { return Active.ToUShort(); }}

		public InterpretationStateEventArgs(int roomId, ushort boothId, bool active)
		{
			RoomId = roomId;
			BoothId = boothId;
			Active = active;
		}

		/// <summary>
		/// Empty constructor so S+ can see the class.
		/// </summary>
		public InterpretationStateEventArgs()
		{}
	}
}