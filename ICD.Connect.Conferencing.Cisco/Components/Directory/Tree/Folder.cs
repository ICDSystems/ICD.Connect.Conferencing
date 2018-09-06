using System.Collections.Generic;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory.Tree
{
	/// <summary>
	/// FolderComponent represents a folder in the phonebook.
	/// </summary>
	public sealed class Folder : AbstractFolder
	{
		private readonly string m_Name;

		#region Properties

		/// <summary>
		/// Gets the name.
		/// </summary>
		public override string Name { get { return m_Name; } }

		/// <summary>
		/// Gets the phonebook type.
		/// </summary>
		public override ePhonebookType PhonebookType
		{
			get { return (FolderId.StartsWith("local")) ? ePhonebookType.Local : ePhonebookType.Corporate; }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="folderId"></param>
		public Folder(string name, string folderId) : base(folderId)
		{
			m_Name = name;
		}

		/// <summary>
		/// Creates a Folder from a Folder XML Element.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="idPrefix"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static Folder FromXml(string xml, string idPrefix, Dictionary<string, IFolder> cache)
		{
			// From CE 9 onwards, LocalId is a child element, but from 8 back, it was an attribute.
			// So, try to get the child element first, and if not, load the attribute value instead.
			string folderId = XmlUtils.TryReadChildElementContentAsString(xml, "LocalId") ??
			                  XmlUtils.GetAttribute(xml, "localId").Value;
			string cachedId = idPrefix + folderId;

			if (!cache.ContainsKey(cachedId))
			{
				string name = XmlUtils.TryReadChildElementContentAsString(xml, "Name");
				cache[cachedId] = new Folder(name, folderId);
			}

			return cache[cachedId] as Folder;
		}

		#endregion
	}
}
