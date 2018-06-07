using System;
using ICD.Connect.Conferencing.Directory.Tree;
using ICD.Connect.Devices.Proxies.Controls;
using ICD.Connect.Devices.Proxies.Devices;

namespace ICD.Connect.Conferencing.Proxies.Controls.Directory
{
	public abstract class AbstractProxyDirectoryControl : AbstractProxyDeviceControl, IProxyDirectoryControl
	{
		/// <summary>
		/// Raised when the directory tree is cleared.
		/// </summary>
		public event EventHandler OnCleared;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractProxyDirectoryControl(IProxyDeviceBase parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Gets the root folder for the directory.
		/// </summary>
		/// <returns></returns>
		public IDirectoryFolder GetRoot()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Clears the cached directory for repopulation.
		/// </summary>
		public void Clear()
		{
			CallMethod(DirectoryControlApi.METHOD_CLEAR);
		}

		/// <summary>
		/// Begin caching the child elements of the given folder.
		/// </summary>
		/// <param name="folder"></param>
		public void PopulateFolder(IDirectoryFolder folder)
		{
			throw new NotImplementedException();
		}
	}
}
