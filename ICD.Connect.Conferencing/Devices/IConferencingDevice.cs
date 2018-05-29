using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Devices
{
	public interface IConferencingDevice : IDevice
	{
		/// <summary>
		/// Configured information about how the input connectors should be used.
		/// </summary>
		CodecInputTypes InputTypes { get; }
	}
}
