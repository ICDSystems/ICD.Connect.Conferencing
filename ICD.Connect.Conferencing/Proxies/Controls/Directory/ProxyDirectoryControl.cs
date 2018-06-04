using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Directory
{
	public sealed class ProxyDirectoryControl : AbstractProxyDirectoryControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ProxyDirectoryControl(IProxyDeviceBase parent, int id)
			: base(parent, id)
		{
		}
	}
}
