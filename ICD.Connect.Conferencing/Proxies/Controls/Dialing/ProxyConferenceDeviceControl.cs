using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Dialing
{
	public sealed class ProxyConferenceDeviceControl : AbstractProxyConferenceDeviceControl<IConference>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ProxyConferenceDeviceControl(IProxyDevice parent, int id)
			: base(parent, id)
		{
		}
	}
}
