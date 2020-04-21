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
		public ProxyVideoConferenceRouteControl(IProxyDevice parent, int id)
			: base(parent, id)
		{
		}
	}
}
