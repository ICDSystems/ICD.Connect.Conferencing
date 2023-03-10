using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Devices
{
	public interface IVideoConferenceDevice : IDevice
	{
		/// <summary>
		/// Configured information about how the input connectors should be used.
		/// </summary>
		CodecInputTypes InputTypes { get; }

		/// <summary>
		/// The default camera used by the conference device.
		/// </summary>
		IDeviceBase DefaultCamera { get; set; }
	}
}
