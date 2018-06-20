using System;
using System.Collections.Generic;
using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.Directory.Tree
{
	/// <summary>
	/// Interface for all directory folders.
	/// </summary>
	public interface IDirectoryFolder
	{
		/// <summary>
		/// Called when a child contact/folder is added or removed.
		/// </summary>
		event EventHandler OnContentsChanged;

		#region Properties

		/// <summary>
		/// Returns the number of child folders.
		/// </summary>
		int FolderCount { get; }

		/// <summary>
		/// Returns the number of child contacts.
		/// </summary>
		int ContactCount { get; }

		/// <summary>
		/// Returns the total number of children.
		/// </summary>
		int ChildCount { get; }

		/// <summary>
		/// Name of the folder.
		/// </summary>
		string Name { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Clears all children from the folder.
		/// </summary>
		void Clear();

		/// <summary>
		/// Recursively clears all children from the folder and its children.
		/// </summary>
		void ClearRecursive();

		/// <summary>
		/// Gets the cached folders.
		/// </summary>
		/// <returns></returns>
		IDirectoryFolder[] GetFolders();

		/// <summary>
		/// Gets the cached folder with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		IDirectoryFolder GetFolder(string name);

		/// <summary>
		/// Gets the cached contacts.
		/// </summary>
		/// <returns></returns>
		IContact[] GetContacts();

		/// <summary>
		/// Gets the cached contact with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		IContact GetContact(string name);

		/// <summary>
		/// Caches the folder.
		/// </summary>
		/// <param name="folder"></param>
		bool AddFolder(IDirectoryFolder folder);

		/// <summary>
		/// Caches the folders.
		/// </summary>
		/// <param name="folders"></param>
		bool AddFolders(IEnumerable<IDirectoryFolder> folders);

		/// <summary>
		/// Caches the contact.
		/// </summary>
		/// <param name="contact"></param>
		bool AddContact(IContact contact);

		/// <summary>
		/// Caches the contacts.
		/// </summary>
		/// <param name="contacts"></param>
		bool AddContacts(IEnumerable<IContact> contacts);

		/// <summary>
		/// Gets this IFolder and all child folders recursively.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IDirectoryFolder> Recurse();

		#endregion
	}
}
