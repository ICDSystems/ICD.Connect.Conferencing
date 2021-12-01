using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialContexts;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree
{
	/// <summary>
	/// Contact provides information for a phonebook contact.
	/// </summary>
	[XmlConverter(typeof(CiscoContactXmlConverter))]
	public sealed class CiscoContact : IContact
	{
		private readonly List<CiscoContactDialContext> m_ContactMethods;

		#region Properties

		public int ItemNumber { get; set; }

		/// <summary>
		/// Gets the contact id.
		/// </summary>
		public string ContactId { get; set; }

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets the folder id.
		/// </summary>
		public string FolderId { get; set; }

		/// <summary>
		/// Gets the phonebook type.
		/// </summary>
		public ePhonebookType PhonebookType
		{
			get { return ContactId != null && ContactId.Contains("local") ? ePhonebookType.Local : ePhonebookType.Corporate; }
		}

		/// <summary>
		/// Gets the title.
		/// </summary>
		public string Title { get; set; }

		string IContact.Name { get { return Name; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public CiscoContact()
		{
			m_ContactMethods = new List<CiscoContactDialContext>();
		}

		/// <summary>
		/// Gets the contact methods.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<CiscoContactDialContext> GetContactMethods()
		{
			return m_ContactMethods.ToArray(m_ContactMethods.Count);
		}

		/// <summary>
		/// Adds the given contact method.
		/// </summary>
		/// <param name="contactDialContext"></param>
		public void AddContactMethod(CiscoContactDialContext contactDialContext)
		{
			if (contactDialContext == null)
				throw new ArgumentNullException("contactDialContext");

			m_ContactMethods.Add(contactDialContext);
		}

		IEnumerable<IDialContext> IContact.GetDialContexts()
		{
			return GetContactMethods().Cast<IDialContext>();
		}
	}

	public sealed class CiscoContactXmlConverter : AbstractGenericXmlConverter<CiscoContact>
	{
		// <Contact item="2">
		//   <Name item="1">Brett Fisher</Name>
		//   <ContactId item="1">localContactId-11</ContactId>
		//   <FolderId item="1">localGroupId-19</FolderId>
		//   <Title item="1">Vice President</Title>
		//   <ContactMethod item="1">
		//     <ContactMethodId item="1">1</ContactMethodId>
		//     <Number item="1">112</Number>
		//     <CallType item="1">Video</CallType>
		//   </ContactMethod>
		// </Contact>

		/// <summary>
		/// Creates a new instance of T.
		/// </summary>
		/// <returns></returns>
		protected override CiscoContact Instantiate()
		{
			return new CiscoContact();
		}

		/// <summary>
		/// Override to handle the current attribute.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		protected override void ReadAttribute(IcdXmlReader reader, CiscoContact instance)
		{
			switch (reader.Name)
			{
				case "item":
					instance.ItemNumber = Int32.Parse(reader.Value);
					break;
				default:
					base.ReadAttribute(reader, instance);
					break;
			}
		}

		/// <summary>
		/// Override to handle the current element.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		protected override void ReadElement(IcdXmlReader reader, CiscoContact instance)
		{
			switch (reader.Name)
			{
				case "Name":
					instance.Name = reader.ReadElementContentAsString();
					break;

				case "ContactId":
					instance.ContactId = reader.ReadElementContentAsString();
					break;

				case "FolderId":
					instance.FolderId = reader.ReadElementContentAsString();
					break;

				case "Title":
					instance.Title = reader.ReadElementContentAsString();
					break;

				case "ContactMethod":
					CiscoContactDialContext contactDialContext = IcdXmlConvert.DeserializeObject<CiscoContactDialContext>(reader);
					instance.AddContactMethod(contactDialContext);
					break;

				default:
					base.ReadElement(reader, instance);
					break;
			}
		}
	}
}
