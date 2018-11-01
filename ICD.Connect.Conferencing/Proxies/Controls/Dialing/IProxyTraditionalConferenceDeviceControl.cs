using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Devices.Proxies.Controls;

namespace ICD.Connect.Conferencing.Proxies.Controls.Dialing
{
	public interface IProxyTraditionalConferenceDeviceControl : IProxyDeviceControl,
		IConferenceDeviceControl<ITraditionalConference>
	{
	}
}
