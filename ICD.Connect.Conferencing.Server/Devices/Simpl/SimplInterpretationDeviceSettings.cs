using ICD.Connect.Devices.Simpl;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server.Devices.Simpl
{
	[KrangSettings("SimplInterpretationDevice", typeof(SimplInterpretationDevice))]
	public sealed class SimplInterpretationDeviceSettings : AbstractSimplDeviceSettings, IInterpretationDeviceSettings
	{
	}
}
