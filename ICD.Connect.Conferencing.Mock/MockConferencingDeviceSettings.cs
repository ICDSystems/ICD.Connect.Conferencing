using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Mock
{
	[KrangSettings("MockConferencingDevice", typeof(MockConferencingDevice))]
	public sealed class MockConferencingDeviceSettings : AbstractDeviceSettings, IMockConferencingDeviceSettings
	{
	}
}
