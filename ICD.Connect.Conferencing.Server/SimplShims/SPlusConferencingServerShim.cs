using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Server.Devices.Server;
using ICD.Connect.Devices.SPlusShims;

namespace ICD.Connect.Conferencing.Server.SimplShims
{
	public sealed class SPlusConferencingServerShim : AbstractSPlusDeviceShim<IConferencingServerDevice>
	{
		[PublicAPI("S+")]
		public void BeginInterpretation(ushort roomId, ushort boothId)
		{
			Originator.BeginInterpretation(roomId, boothId);
		}

		[PublicAPI("S+")]
		public void EndInterpretation(ushort roomId, ushort boothId)
		{
			Originator.EndInterpretation(roomId, boothId);
		}

		[PublicAPI("S+")]
		public ushort[] GetAvailableBoothIds()
		{
			int[] available = Originator.GetAvailableBoothIds().ToArray();
			IEnumerable<ushort> availableAsUShorts = available.Where(value => value >= ushort.MinValue && value <= ushort.MaxValue)
															  .Select(value => (ushort)value);

			return availableAsUShorts.ToArray();
		}

		[PublicAPI("S+")]
		public ushort[] GetAvailableRoomIds()
		{
			int[] available = Originator.GetAvailableRoomIds().ToArray();
			IEnumerable<ushort> availableAsUShorts = available.Where(value => value >= ushort.MinValue && value <= ushort.MaxValue)
															  .Select(value => (ushort)value);

			return availableAsUShorts.ToArray();
		}
	}
}