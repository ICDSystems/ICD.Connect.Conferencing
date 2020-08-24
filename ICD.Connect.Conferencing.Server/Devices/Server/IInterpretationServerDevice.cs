using System;
using System.Collections.Generic;
using ICD.Common.Properties;
using ICD.Connect.Devices.CrestronSPlus.Devices.SPlus;

namespace ICD.Connect.Conferencing.Server.Devices.Server
{
	public interface IInterpretationServerDevice : ISPlusDevice
	{
		event EventHandler<InterpretationStateEventArgs> OnInterpretationStateChanged;
		event EventHandler<InterpretationRoomInfoArgs> OnRoomAdded;

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

		/// <summary>
		/// Gets the Room Name for a given Room Id
		/// </summary>
		/// <param name="roomId"></param>
		/// <returns></returns>
		[PublicAPI]
		string GetRoomName(int roomId);

		/// <summary>
		/// Gets the Room Prefix for a given Room Id
		/// </summary>
		/// <param name="roomId"></param>
		/// <returns></returns>
		[PublicAPI]
		string GetRoomPrefix(int roomId);

		/// <summary>
		/// Gets the Booth Id for a given Room Id
		/// </summary>
		/// <param name="roomId"></param>
		/// <returns></returns>
		[PublicAPI]
		ushort GetBoothId(int roomId);

		/// <summary>
		/// Gets if the room exists
		/// </summary>
		/// <param name="roomId"></param>
		/// <returns></returns>
		[PublicAPI]
		ushort GetRoomExists(int roomId);
	}
}