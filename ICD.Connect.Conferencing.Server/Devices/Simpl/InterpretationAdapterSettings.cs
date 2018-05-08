using ICD.Connect.Devices.Simpl;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server.Devices.Simpl
{
	[KrangSettings("SimplInterpretationAdapter", typeof(SimplInterpretationAdapter))]
	public sealed class InterpretationAdapterSettings : AbstractSimplDeviceSettings, IInterpretationAdapterSettings
	{
	}
}
