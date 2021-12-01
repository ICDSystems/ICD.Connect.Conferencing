using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory
{
	[XmlConverter(typeof(PhonebookSearchResultXmlConverter))]
	public sealed class PhonebookSearchResult
	{
		private readonly List<CiscoFolder> m_Folders;
		private readonly List<CiscoContact> m_Contacts;

		public int Count { get { return m_Folders.Count + m_Contacts.Count; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public PhonebookSearchResult()
		{
			m_Folders = new List<CiscoFolder>();
			m_Contacts = new List<CiscoContact>();
		}

		/// <summary>
		/// Gets the folders.
		/// </summary>
		/// <returns></returns>
		public CiscoFolder[] GetFolders()
		{
			return m_Folders.ToArray(m_Folders.Count);
		}

		/// <summary>
		/// Gets the contacts.
		/// </summary>
		/// <returns></returns>
		public CiscoContact[] GetContacts()
		{
			return m_Contacts.ToArray(m_Contacts.Count);
		}

		/// <summary>
		/// Adds the given folder.
		/// </summary>
		/// <param name="folder"></param>
		public void AddFolder(CiscoFolder folder)
		{
			if (folder == null)
				throw new ArgumentNullException("folder");

			m_Folders.Add(folder);
		}

		/// <summary>
		/// Adds the given contact.
		/// </summary>
		/// <param name="contact"></param>
		public void AddContact(CiscoContact contact)
		{
			if (contact == null)
				throw new ArgumentNullException("contact");

			m_Contacts.Add(contact);
		}
	}

	public sealed class PhonebookSearchResultXmlConverter : AbstractGenericXmlConverter<PhonebookSearchResult>
	{
		// <PhonebookSearchResult item="1" status="OK">
		//   <ResultInfo item="1">
		//     <Offset item="1">0</Offset>
		//     <Limit item="1">50</Limit>
		//     <TotalRows item="1">21</TotalRows>
		//   </ResultInfo>
		//   <Folder item="1" localId="localGroupId-3">
		//     <Name item="1">CA</Name>
		//     <FolderId item="1">localGroupId-3</FolderId>
		//     <ParentFolderId item="1">localGroupId-2</ParentFolderId>
		//   </Folder>
		//   <Contact item="1">
		//     <Name item="1">Bradd Fisher</Name>
		//     <ContactId item="1">localContactId-10</ContactId>
		//     <FolderId item="1">localGroupId-18</FolderId>
		//     <Title item="1">President</Title>
		//     <ContactMethod item="1">
		//       <ContactMethodId item="1">1</ContactMethodId>
		//       <Number item="1">111</Number>
		//       <CallType item="1">Video</CallType>
		//     </ContactMethod>
		//   </Contact>
		// </PhonebookSearchResult>

		/// <summary>
		/// Creates a new instance of T.
		/// </summary>
		/// <returns></returns>
		protected override PhonebookSearchResult Instantiate()
		{
			return new PhonebookSearchResult();
		}

		/// <summary>
		/// Override to handle the current element.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		protected override void ReadElement(IcdXmlReader reader, PhonebookSearchResult instance)
		{
			switch (reader.Name)
			{
				case "Folder":
					CiscoFolder folder = IcdXmlConvert.DeserializeObject<CiscoFolder>(reader);
					instance.AddFolder(folder);
					break;

				case "Contact":
					CiscoContact contact = IcdXmlConvert.DeserializeObject<CiscoContact>(reader);
					instance.AddContact(contact);
					break;

				default:
					base.ReadElement(reader, instance);
					break;
			}
		}
	}
}
