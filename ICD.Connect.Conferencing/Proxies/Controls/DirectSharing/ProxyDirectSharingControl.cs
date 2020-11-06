using System;
using ICD.Common.Properties;
using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.DirectSharing
{
	public sealed class ProxyDirectSharingControl : AbstractProxyDirectSharingControl
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ProxyDirectSharingControl([NotNull] IProxyDevice parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		/// <param name="uuid"></param>
		public ProxyDirectSharingControl([NotNull] IProxyDevice parent, int id, Guid uuid)
			: base(parent, id, uuid)
		{
		}
	}
}
