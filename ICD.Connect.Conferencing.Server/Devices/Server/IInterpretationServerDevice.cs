using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Server.Devices.Server
{
	public interface IInterpretationServerDevice : IDevice
	{
		event EventHandler<InterpretationStateEventArgs> OnInterpretationStateChanged;

		/// <summary>
		/// Gets the rooms which are registered with the core, 
		/// but do not currently have interpretation active.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		IEnumerable<int> GetAvailableRoomIds();

		/// <summary>
		/// Gets the booths that are not currently interpreting for any rooms.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		IEnumerable<ushort> GetAvailableBoothIds();

		/// <summary>
		/// Begins forwarding 
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="boothId"></param>
		[PublicAPI]
		void BeginInterpretation(int roomId, ushort boothId);

		/// <summary>
		/// Ends forwarding 
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="boothId"></param>
		[PublicAPI]
		void EndInterpretation(int roomId, ushort boothId);
	}
}