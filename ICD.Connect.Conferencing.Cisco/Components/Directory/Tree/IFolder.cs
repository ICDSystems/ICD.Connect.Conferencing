using System;
using System.Collections.Generic;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory.Tree
{
	/// <summary>
	/// Interface for all directory folders.
	/// </summary>
	public interface IFolder : INode
	{
		/// <summary>
		/// Called when a child contact/folder is added or removed.
		/// </summary>
		event EventHandler OnContentsChanged;

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
		/// The result id for browsing.
		/// </summary>
		string FolderSearchId { get; }

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
		IFolder[] GetFolders();

		/// <summary>
		/// Gets the cached folder at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		IFolder GetFolder(int index);

		/// <summary>
		/// Gets the cached contacts.
		/// </summary>
		/// <returns></returns>
		CiscoContact[] GetContacts();

		/// <summary>
		/// Gets the cached contact at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		CiscoContact GetContact(int index);

		/// <summary>
		/// Gets the cached child at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		INode GetChild(int index);

		/// <summary>
		/// Gets all of the cached children.
		/// </summary>
		/// <returns></returns>
		INode[] GetChildren();

		/// <summary>
		/// Adds the folders and contacts to the folder.
		/// </summary>
		/// <param name="folders"></param>
		/// <param name="contacts"></param>
		bool AddChildren(IEnumerable<IFolder> folders, IEnumerable<CiscoContact> contacts);

		/// <summary>
		/// Caches the folder.
		/// </summary>
		/// <param name="folder"></param>
		bool AddFolder(IFolder folder);

		/// <summary>
		/// Caches the folders.
		/// </summary>
		/// <param name="folders"></param>
		bool AddFolders(IEnumerable<IFolder> folders);

		/// <summary>
		/// Caches the contact.
		/// </summary>
		/// <param name="contact"></param>
		bool AddContact(CiscoContact contact);

		/// <summary>
		/// Caches the contacts.
		/// </summary>
		/// <param name="contacts"></param>
		bool AddContacts(IEnumerable<CiscoContact> contacts);

		/// <summary>
		/// Gets the search command for the contents of the folder.
		/// </summary>
		/// <returns></returns>
		string GetSearchCommand();

		/// <summary>
		/// Gets this IFolder and all child folders recursively.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IFolder> Recurse();
	}
}
