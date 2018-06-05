using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Conferencing.Directory.Tree;
using ICD.Connect.Conferencing.Proxies.Controls.Directory;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Directory
{
	[ApiClass(typeof(ProxyDirectoryControl), typeof(IDeviceControl))]
	public interface IDirectoryControl : IDeviceControl
	{
		/// <summary>
		/// Raised when the directory tree is cleared.
		/// </summary>
		[ApiEvent(DirectoryControlApi.EVENT_CLEARED, DirectoryControlApi.HELP_EVENT_CLEARED)]
		event EventHandler OnCleared;

		/// <summary>
		/// Gets the root folder for the directory.
		/// </summary>
		/// <returns></returns>
		IDirectoryFolder GetRoot();

		/// <summary>
		/// Clears the cached directory for repopulation.
		/// </summary>
		[ApiMethod(DirectoryControlApi.METHOD_CLEAR, DirectoryControlApi.HELP_METHOD_CLEAR)]
		void Clear();

		/// <summary>
		/// Begin caching the child elements of the given folder.
		/// </summary>
		/// <param name="folder"></param>
		void PopulateFolder(IDirectoryFolder folder);
	}
}
