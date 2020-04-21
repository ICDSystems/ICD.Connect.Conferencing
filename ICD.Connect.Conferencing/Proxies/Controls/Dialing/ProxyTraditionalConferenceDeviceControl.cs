using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Dialing
{
	public sealed class ProxyTraditionalConferenceDeviceControl : AbstractProxyTraditionalConferenceDeviceControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ProxyTraditionalConferenceDeviceControl(IProxyDevice parent, int id)
			: base(parent, id)
		{
		}
	}
}
