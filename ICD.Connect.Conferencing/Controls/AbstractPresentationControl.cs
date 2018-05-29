using ICD.Connect.Devices;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls
{
	public abstract class AbstractPresentationControl<TDevice> : AbstractDeviceControl<TDevice>, IPresentationControl
		where TDevice : IDeviceBase
	{
	}
}
