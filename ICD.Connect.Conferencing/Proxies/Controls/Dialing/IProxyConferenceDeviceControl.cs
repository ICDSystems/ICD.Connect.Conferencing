using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Devices.Proxies.Controls;

namespace ICD.Connect.Conferencing.Proxies.Controls.Dialing
{
	public interface IProxyConferenceDeviceControl : IProxyDeviceControl, IConferenceDeviceControl
	{
	}

	public interface IProxyConferenceDeviceControl<T> : IProxyConferenceDeviceControl, IConferenceDeviceControl<T>
		where T : IConference
	{
	}
}
