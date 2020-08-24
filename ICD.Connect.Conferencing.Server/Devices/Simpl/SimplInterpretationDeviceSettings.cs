using ICD.Connect.Devices.CrestronSPlus.Devices.SPlus;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server.Devices.Simpl
{
	[KrangSettings("SimplInterpretationDevice", typeof(SimplInterpretationDevice))]
	public sealed class SimplInterpretationDeviceSettings : AbstractSPlusDeviceSettings, ISimplInterpretationDeviceSettings
	{
	}
}
