using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.BYOD
{
	public interface IByodHubDeviceSettings : IDeviceSettings
	{
		/// <summary>
		/// Originator ID of a destination device for cameras to be routed to through the BYOD Hub Device.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(IDevice))]
		int? CameraDestinationDevice { get; set; }
	}
}
