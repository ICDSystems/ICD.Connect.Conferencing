using System;
using System.Collections.Generic;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory.Tree
{
	/// <summary>
	/// FolderComponent represents a folder in the phonebook.
	/// </summary>
	public abstract class AbstractCiscoFolder : AbstractFolder, INode
	{
		private readonly string m_FolderId;
		private readonly string m_FolderSearchId;

		#region Properties

		/// <summary>
		/// The id of the folder.
		/// </summary>
		public string FolderId { get { return m_FolderId; } }

		/// <summary>
		/// The result id for browsing.
		/// </summary>
		public string FolderSearchId { get { return m_FolderSearchId; } }

		/// <summary>
		/// Gets the phonebook type.
		/// </summary>
		public ePhonebookType PhonebookType
		{
			get { return (FolderId.StartsWith("local")) ? ePhonebookType.Local : ePhonebookType.Corporate; }
		}

		protected override IComparer<IContact> ContactComparer
		{
			get { return CiscoContactComparer.Instance; }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="folderId"></param>
		public AbstractCiscoFolder(string folderId) : base()
		{
			m_FolderId = folderId;

			m_FolderSearchId = Guid.NewGuid().ToString();
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

		#endregion

		#region Protected Methods

		protected override bool AddFolder(IFolder folder, bool raise)
		{
			AbstractCiscoFolder abstractCiscoFolder = folder as AbstractCiscoFolder;
			if (abstractCiscoFolder == null || abstractCiscoFolder.PhonebookType != PhonebookType)
				return false;
			return base.AddFolder(folder, raise);
		}

		protected override bool AddContact(IContact contact, bool raise)
		{
			CiscoContact ciscoContact = contact as CiscoContact;
			if (ciscoContact == null || ciscoContact.PhonebookType != PhonebookType)
				return false;
			return base.AddContact(contact, raise);
		}

		#endregion
	}

	internal sealed class CiscoContactComparer : IComparer<IContact>
	{
		private static CiscoContactComparer s_Instance;

		public static CiscoContactComparer Instance { get { return s_Instance = s_Instance ?? new CiscoContactComparer(); } }

		public int Compare(IContact x, IContact y)
		{
			if (x == null)
				throw new ArgumentNullException("x");

			if (y == null)
				throw new ArgumentNullException("y");

			CiscoContact ciscoX = x as CiscoContact;
			CiscoContact ciscoY = y as CiscoContact;
			if (ciscoX == null || ciscoY == null)
				throw new InvalidOperationException(string.Format("{0} cannot compare types {1} and {2}",
					GetType().Name, x.GetType().Name, y.GetType().Name));

			int surname = string.Compare(ciscoX.LastName, ciscoY.LastName, StringComparison.Ordinal);
			return surname != 0
					   ? surname
					   : string.Compare(ciscoX.FirstName, ciscoY.FirstName, StringComparison.Ordinal);
		}
	}
}
