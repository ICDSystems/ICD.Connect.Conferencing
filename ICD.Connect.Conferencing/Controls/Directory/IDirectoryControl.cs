using System;
using ICD.Connect.Conferencing.Directory.Tree;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Directory
{
	public interface IDirectoryControl : IDeviceControl
	{
		/// <summary>
		/// Raised when the directory tree is cleared.
		/// </summary>
		event EventHandler OnCleared;

		/// <summary>
		/// Gets the root folder for the directory.
		/// </summary>
		/// <returns></returns>
		IDirectoryFolder GetRoot();

		/// <summary>
		/// Clears the cached directory for repopulation.
		/// </summary>
		void Clear();
	}
}
