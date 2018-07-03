using System;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree
{
	/// <summary>
	/// FolderComponent represents a folder in the phonebook.
	/// </summary>
	public abstract class AbstractCiscoFolder : AbstractDirectoryFolder
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
		public virtual ePhonebookType PhonebookType
		{
			get { return FolderId.StartsWith("local") ? ePhonebookType.Local : ePhonebookType.Corporate; }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="folderId"></param>
		protected AbstractCiscoFolder(string folderId)
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
	}
}
