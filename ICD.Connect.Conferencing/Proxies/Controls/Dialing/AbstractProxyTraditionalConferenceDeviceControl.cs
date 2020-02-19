using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Dialing
{
	public abstract class AbstractProxyTraditionalConferenceDeviceControl : AbstractProxyConferenceDeviceControl<ITraditionalConference>, IProxyTraditionalConferenceDeviceControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractProxyTraditionalConferenceDeviceControl(IProxyDeviceBase parent, int id)
			: base(parent, id)
		{
		}
	}
}
