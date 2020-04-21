using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Layout
{
	public sealed class ProxyConferenceLayoutControl : AbstractProxyConferenceLayoutControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ProxyConferenceLayoutControl(IProxyDevice parent, int id)
			: base(parent, id)
		{
		}
	}
}
