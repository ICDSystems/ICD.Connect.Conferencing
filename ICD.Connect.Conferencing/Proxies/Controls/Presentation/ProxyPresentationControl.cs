using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Presentation
{
	public sealed class ProxyPresentationControl : AbstractProxyPresentationControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ProxyPresentationControl(IProxyDevice parent, int id)
			: base(parent, id)
		{
		}
	}
}
