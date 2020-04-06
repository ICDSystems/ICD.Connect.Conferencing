using ICD.Connect.Devices.Mock;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Mock
{
	[KrangSettings("MockConferencingDevice", typeof(MockConferencingDevice))]
	public sealed class MockConferencingDeviceSettings : AbstractMockDeviceSettings, IMockConferencingDeviceSettings
	{
	}
}
