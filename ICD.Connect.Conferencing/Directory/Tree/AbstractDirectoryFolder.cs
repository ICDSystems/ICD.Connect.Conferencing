using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.Directory.Tree
{
	/// <summary>
	/// AbstractFolder provides shared functionality between Folder and RootFolder.
	/// </summary>
	public abstract class AbstractDirectoryFolder : IDirectoryFolder
	{
		/// <summary>
		/// Called when a child contact/folder is added or removed.
		/// </summary>
		public event EventHandler OnContentsChanged;

		private readonly IComparer<IDirectoryFolder> m_FolderComparer;
		private readonly IComparer<IContact> m_ContactComparer;

		private readonly BiDictionary<string, IDirectoryFolder> m_NameToFolder;
		private readonly BiDictionary<string, IContact> m_NameToContact;

		private readonly List<IDirectoryFolder> m_FoldersSorted;
		private readonly List<IContact> m_ContactsSorted;

		private readonly SafeCriticalSection m_FoldersSection;
		private readonly SafeCriticalSection m_ContactsSection;

		#region Properties

		/// <summary>
		/// The name of the folder.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// Gets the number of cached child contacts.
		/// </summary>
		public int ContactCount { get { return m_ContactsSection.Execute(() => m_NameToContact.Count); } }

		/// <summary>
		/// Gets the number of cached child folders.
		/// </summary>
		public int FolderCount { get { return m_FoldersSection.Execute(() => m_NameToFolder.Count); } }

		/// <summary>
		/// Gets the number of child folders and contacts.
		/// </summary>
		public int ChildCount { get { return FolderCount + ContactCount; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractDirectoryFolder(IComparer<IDirectoryFolder> folderComparer, IComparer<IContact> contactComparer)
		{
			if (folderComparer == null)
				throw new ArgumentNullException("folderComparer");

			if (contactComparer == null)
				throw new ArgumentNullException("contactComparer");

			m_FolderComparer = folderComparer;
			m_ContactComparer = contactComparer;

			m_NameToFolder = new BiDictionary<string, IDirectoryFolder>();
			m_NameToContact = new BiDictionary<string, IContact>();

			m_FoldersSorted = new List<IDirectoryFolder>();
			m_ContactsSorted = new List<IContact>();

			m_FoldersSection = new SafeCriticalSection();
			m_ContactsSection = new SafeCriticalSection();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Clears all children from the folder.
		/// </summary>
		public void Clear()
		{
			bool raise;

			m_ContactsSection.Enter();
			m_FoldersSection.Enter();

			try
			{
				raise = m_FoldersSorted.Count > 0 || m_ContactsSorted.Count > 0;

				m_NameToContact.Clear();
				m_NameToFolder.Clear();

				m_FoldersSorted.Clear();
				m_ContactsSorted.Clear();
			}
			finally
			{
				m_ContactsSection.Leave();
				m_FoldersSection.Leave();
			}

			if (raise)
				OnContentsChanged.Raise(this);
		}

		/// <summary>
		/// Recursively clears all children from the folder and its children.
		/// </summary>
		public void ClearRecursive()
		{
			foreach (IDirectoryFolder folder in Recurse().Reverse())
				folder.Clear();
		}

		/// <summary>
		/// Gets the cached folders.
		/// </summary>
		/// <returns></returns>
		public IDirectoryFolder[] GetFolders()
		{
			return m_FoldersSection.Execute(() => m_FoldersSorted.ToArray(m_FoldersSorted.Count));
		}

		/// <summary>
		/// Gets the cached folder with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public IDirectoryFolder GetFolder(string name)
		{
			return m_FoldersSection.Execute(() => m_NameToFolder.GetDefault(name));
		}

		/// <summary>
		/// Gets the cached contacts.
		/// </summary>
		/// <returns></returns>
		public IContact[] GetContacts()
		{
			return m_ContactsSection.Execute(() => m_ContactsSorted.ToArray(m_ContactsSorted.Count));
		}

		/// <summary>
		/// Gets the cached contact.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public IContact GetContact(string name)
		{
			return m_ContactsSection.Execute(() => m_NameToContact.GetDefault(name));
		}

		/// <summary>
		/// Adds the folders and contacts to the folder.
		/// </summary>
		/// <param name="folders"></param>
		/// <param name="contacts"></param>
		public bool AddChildren(IEnumerable<IDirectoryFolder> folders, IEnumerable<IContact> contacts)
		{
			if (folders == null)
				throw new ArgumentNullException("folders");

			if (contacts == null)
				throw new ArgumentNullException("contacts");

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
		public bool AddFolder(IDirectoryFolder folder)
		{
			if (folder == null)
				throw new ArgumentNullException("folder");

			return AddFolder(folder, true);
		}

		/// <summary>
		/// Caches the folders.
		/// </summary>
		/// <param name="folders"></param>
		public bool AddFolders(IEnumerable<IDirectoryFolder> folders)
		{
			if (folders == null)
				throw new ArgumentNullException("folders");

			return AddFolders(folders, true);
		}

		/// <summary>
		/// Caches the contact.
		/// </summary>
		/// <param name="contact"></param>
		public bool AddContact(IContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			return AddContact(contact, true);
		}

		/// <summary>
		/// Caches the contacts.
		/// </summary>
		/// <param name="contacts"></param>
		public bool AddContacts(IEnumerable<IContact> contacts)
		{
			if (contacts == null)
				throw new ArgumentNullException("contacts");

			return AddContacts(contacts, true);
		}

		/// <summary>
		/// Returns true if this folder contains the given folder.
		/// </summary>
		/// <param name="folder"></param>
		/// <returns></returns>
		public bool ContainsFolder(IDirectoryFolder folder)
		{
			if (folder == null)
				throw new ArgumentNullException("folder");

			return m_FoldersSection.Execute(() => m_NameToFolder.ContainsValue(folder));
		}

		/// <summary>
		/// Gets this IFolder and all child folders recursively.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IDirectoryFolder> Recurse()
		{
			Queue<IDirectoryFolder> toProcess = new Queue<IDirectoryFolder>();
			toProcess.Enqueue(this);

			while (toProcess.Count > 0)
			{
				IDirectoryFolder parent = toProcess.Dequeue();
				yield return parent;

				foreach (IDirectoryFolder folder in parent.GetFolders())
					toProcess.Enqueue(folder);
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Caches the folders.
		/// </summary>
		/// <param name="folders"></param>
		/// <param name="raise"></param>
		private bool AddFolders(IEnumerable<IDirectoryFolder> folders, bool raise)
		{
			if (folders == null)
				throw new ArgumentNullException("folders");

			bool output = false;

			foreach (IDirectoryFolder folder in folders)
				output |= AddFolder(folder, false);

			if (raise && output)
				OnContentsChanged.Raise(this);

			return output;
		}

		private bool AddFolder(IDirectoryFolder folder, bool raise)
		{
			if (folder == null)
				throw new ArgumentNullException("folder");

			m_FoldersSection.Enter();

			try
			{
				if (m_NameToFolder.ContainsValue(folder) || m_NameToFolder.ContainsKey(folder.Name))
					return false;

				m_NameToFolder.Add(folder.Name, folder);
				m_FoldersSorted.AddSorted(folder, m_FolderComparer);
			}
			finally
			{
				m_FoldersSection.Leave();
			}

			if (raise)
				OnContentsChanged.Raise(this);

			return true;
		}

		/// <summary>
		/// Caches the contacts.
		/// </summary>
		/// <param name="contacts"></param>
		/// <param name="raise"></param>
		private bool AddContacts(IEnumerable<IContact> contacts, bool raise)
		{
			if (contacts == null)
				throw new ArgumentNullException("contacts");

			bool output = false;

			foreach (IContact contact in contacts)
				output |= AddContact(contact, false);

			if (raise && output)
				OnContentsChanged.Raise(this);

			return output;
		}

		protected virtual bool AddContact(IContact contact, bool raise)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			m_ContactsSection.Enter();

			try
			{
				if (m_NameToContact.ContainsKey(contact.Name) || m_NameToContact.ContainsValue(contact))
					return false;

				m_NameToContact.Add(contact.Name, contact);
				m_ContactsSorted.AddSorted(contact, m_ContactComparer);
			}
			finally
			{
				m_ContactsSection.Leave();
			}

			if (raise)
				OnContentsChanged.Raise(this);

			return true;
		}

		#endregion
	}
}
