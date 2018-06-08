using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Routing
{
	public sealed class ProxyVideoConferenceRouteControl : AbstractProxyConferenceRouteControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ProxyVideoConferenceRouteControl(IProxyDeviceBase parent, int id)
			: base(parent, id)
		{
		}
	}
}
