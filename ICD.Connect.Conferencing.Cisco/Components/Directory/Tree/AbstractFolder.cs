using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory.Tree
{
	/// <summary>
	/// AbstractFolder provides shared functionality between Folder and RootFolder.
	/// </summary>
	public abstract class AbstractFolder : IFolder
	{
		/// <summary>
		/// Called when a child contact/folder is added or removed.
		/// </summary>
		public event EventHandler OnContentsChanged;

		private readonly IcdHashSet<IFolder> m_CachedFolders;
		private readonly IcdHashSet<CiscoContact> m_CachedContacts;

		private readonly SafeCriticalSection m_FoldersSection;
		private readonly SafeCriticalSection m_ContactsSection;

		#region Properties

		/// <summary>
		/// The name of the folder.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// The id of the folder.
		/// </summary>
		public string FolderId { get; private set; }

		/// <summary>
		/// The result id for browsing.
		/// </summary>
		public string FolderSearchId { get; private set; }

		/// <summary>
		/// Gets the phonebook type.
		/// </summary>
		public abstract ePhonebookType PhonebookType { get; }

		/// <summary>
		/// Gets the number of cached child contacts.
		/// </summary>
		public int ContactCount { get { return m_ContactsSection.Execute(() => m_CachedContacts.Count); } }

		/// <summary>
		/// Gets the number of cached child folders.
		/// </summary>
		public int FolderCount { get { return m_FoldersSection.Execute(() => m_CachedFolders.Count); } }

		/// <summary>
		/// Gets the number of child folders and contacts.
		/// </summary>
		public int ChildCount { get { return FolderCount + ContactCount; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractFolder(string folderId)
		{
			FolderId = folderId;

			m_CachedFolders = new IcdHashSet<IFolder>();
			m_CachedContacts = new IcdHashSet<CiscoContact>();

			m_FoldersSection = new SafeCriticalSection();
			m_ContactsSection = new SafeCriticalSection();

			FolderSearchId = Guid.NewGuid().ToString();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Clears all children from the folder.
		/// </summary>
		public void Clear()
		{
			m_ContactsSection.Execute(() => m_CachedContacts.Clear());
			m_FoldersSection.Execute(() => m_CachedFolders.Clear());

			OnContentsChanged.Raise(this);
		}

		/// <summary>
		/// Recursively clears all children from the folder and its children.
		/// </summary>
		public void ClearRecursive()
		{
			foreach (IFolder folder in Recurse().Reverse())
				folder.Clear();
		}

		/// <summary>
		/// Gets the cached folders.
		/// </summary>
		/// <returns></returns>
		public IFolder[] GetFolders()
		{
			return m_FoldersSection.Execute(() => m_CachedFolders.OrderBy(f => f.Name).ToArray());
		}

		/// <summary>
		/// Gets the cached folder at the given index.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public IFolder GetFolder(int index)
		{
			return GetFolders()[index];
		}

		/// <summary>
		/// Gets the cached contacts.
		/// </summary>
		/// <returns></returns>
		public CiscoContact[] GetContacts()
		{
			return m_ContactsSection.Execute(() => m_CachedContacts.OrderBy(c => c.LastName).ThenBy(c => c.FirstName).ToArray());
		}

		/// <summary>
		/// Gets the cached contact.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public CiscoContact GetContact(int index)
		{
			return GetContacts()[index];
		}

		/// <summary>
		/// Gets the child folder/contact at the given index (folders come first).
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public INode GetChild(int index)
		{
			return GetChildren()[index];
		}

		/// <summary>
		/// Gets all of the cached children.
		/// </summary>
		/// <returns></returns>
		public INode[] GetChildren()
		{
			return GetFolders().Cast<INode>().Concat(GetContacts()).ToArray();
		}

		/// <summary>
		/// Adds the folders and contacts to the folder.
		/// </summary>
		/// <param name="folders"></param>
		/// <param name="contacts"></param>
		public bool AddChildren(IEnumerable<IFolder> folders, IEnumerable<CiscoContact> contacts)
		{
			bool output = AddFolders(folders, false);
			output |= AddContacts(contacts, false);

			if (output)
				OnContentsChanged.Raise(this);

			return output;
		}

		/// <summary>
		/// Caches the folder.
		/// </summary>
		/// <param name="folder"></param>
		public bool AddFolder(IFolder folder)
		{
			return AddFolders(new[] {folder});
		}

		/// <summary>
		/// Caches the folders.
		/// </summary>
		/// <param name="folders"></param>
		public bool AddFolders(IEnumerable<IFolder> folders)
		{
			return AddFolders(folders, true);
		}

		/// <summary>
		/// Caches the contact.
		/// </summary>
		/// <param name="contact"></param>
		public bool AddContact(CiscoContact contact)
		{
			return AddContacts(new[] {contact});
		}

		/// <summary>
		/// Caches the contacts.
		/// </summary>
		/// <param name="contacts"></param>
		public bool AddContacts(IEnumerable<CiscoContact> contacts)
		{
			return AddContacts(contacts, true);
		}

		/// <summary>
		/// Gets the search command for the contents of the folder.
		/// </summary>
		/// <returns></returns>
		public string GetSearchCommand()
		{
			string command = "xcommand phonebook search Limit: 65534 Recursive: False";
			command += " PhonebookType: " + PhonebookType;

			if (!string.IsNullOrEmpty(FolderId))
				command += string.Format(" FolderId: \"{0}\"", FolderId);

			command += string.Format("| resultId=\"{0}\"", FolderSearchId);

			return command;
		}

		/// <summary>
		/// Gets this IFolder and all child folders recursively.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IFolder> Recurse()
		{
			Queue<IFolder> toProcess = new Queue<IFolder>();
			toProcess.Enqueue(this);

			while (toProcess.Count > 0)
			{
				IFolder parent = toProcess.Dequeue();
				yield return parent;

				foreach (IFolder folder in parent.GetFolders())
					toProcess.Enqueue(folder);
			}
		}

		#endregion

		#region Private Methods

		private bool AddFolders(IEnumerable<IFolder> folders, bool raise)
		{
			bool output;

			m_FoldersSection.Enter();

			try
			{
				int count = m_CachedFolders.Count;
				m_CachedFolders.AddRange(folders.Where(f => f.PhonebookType == PhonebookType));
				output = m_CachedFolders.Count != count;
			}
			finally
			{
				m_FoldersSection.Leave();
			}

			if (output && raise)
				OnContentsChanged.Raise(this);

			return output;
		}

		private bool AddContacts(IEnumerable<CiscoContact> contacts, bool raise)
		{
			bool output;

			m_ContactsSection.Enter();

			try
			{
				int count = m_CachedContacts.Count;
				m_CachedContacts.AddRange(contacts.Where(c => c.PhonebookType == PhonebookType));
				output = m_CachedContacts.Count != count;
			}
			finally
			{
				m_ContactsSection.Leave();
			}

			if (output && raise)
				OnContentsChanged.Raise(this);

			return output;
		}

		#endregion
	}
}
