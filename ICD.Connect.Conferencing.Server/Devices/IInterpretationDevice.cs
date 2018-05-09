using ICD.Connect.Conferencing.Devices;

namespace ICD.Connect.Conferencing.Server.Devices
{
	public interface IInterpretationDevice : IDialerDevice
	{
		bool IsInterpretationActive { get; }
	}
}
