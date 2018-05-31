using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Routing
{
	public sealed class ProxyVideoConferenceRouteDestinationControl : AbstractProxyConferenceRouteDestinationControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ProxyVideoConferenceRouteDestinationControl(IProxyDeviceBase parent, int id)
			: base(parent, id)
		{
		}
	}
}
