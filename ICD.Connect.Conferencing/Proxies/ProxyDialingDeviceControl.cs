using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies
{
	public sealed class ProxyDialingDeviceControl : AbstractProxyDialingDeviceControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ProxyDialingDeviceControl(IProxyDeviceBase parent, int id)
			: base(parent, id)
		{
		}
	}
}
