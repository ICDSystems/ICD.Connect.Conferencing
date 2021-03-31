using ICD.Common.Properties;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.BYOD
{
	public interface IByodHubDevice
	{
		/// <summary>
		/// Destination device for cameras routed through a BYOD Hub Device.
		/// </summary>
		[CanBeNull]
		IDevice CameraDestinationDevice { get; set; }
	}
}
