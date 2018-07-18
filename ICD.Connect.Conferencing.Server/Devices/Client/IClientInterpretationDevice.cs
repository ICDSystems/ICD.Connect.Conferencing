using ICD.Connect.Conferencing.Devices;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
	public interface IClientInterpretationDevice : IInterpretationDevice
	{
		string RoomName { get; }
		string RoomPrefix { get; }

		void SetRoomNameIfNullOrEmpty(string name);
		void SetRoomPrefixIfNullOrEmpty(string prefix);
	}
}
