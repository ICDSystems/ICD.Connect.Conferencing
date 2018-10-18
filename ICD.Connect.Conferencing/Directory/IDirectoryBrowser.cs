using System;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Directory
{
	public interface IDirectoryBrowser<TFolder> : IDisposable
		where TFolder : IDirectoryFolder
	{
		/// <summary>
		/// Called when navigating to a different folder.
		/// </summary>
		event EventHandler<DirectoryFolderEventArgs> OnPathChanged;

		/// <summary>
		/// Called when the contents of the current folder change.
		/// </summary>
		event EventHandler OnPathContentsChanged;

		/// <summary>
		/// Returns true if the current folder is the root.
		/// </summary>
		/// <value></value>
		bool IsCurrentFolderRoot { get; }

		/// <summary>
		/// Gets the current folder on the path.
		/// </summary>
		/// <returns></returns>
		TFolder GetCurrentFolder();

		/// <summary>
		/// Pushes the folder onto the path.
		/// </summary>
		/// <param name="folder"></param>
		/// <returns>The new current folder.</returns>
		[PublicAPI]
		TFolder EnterFolder(TFolder folder);

		/// <summary>
		/// Sets the parent folder as the current folder.
		/// </summary>
		/// <returns>The new current folder.</returns>
		[PublicAPI]
		TFolder GoUp();

		/// <summary>
		/// Sets the root folder as the current folder.
		/// </summary>
		/// <returns></returns>
		/// <returns>The new current folder.</returns>
		TFolder GoToRoot();
	}
}