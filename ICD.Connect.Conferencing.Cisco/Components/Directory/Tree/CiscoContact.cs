using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Directory.Tree;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory.Tree
{
	/// <summary>
	/// Contact provides information for a phonebook contact.
	/// </summary>
	public sealed class CiscoContact : INode, IContact
	{
		private readonly IContactMethod[] m_ContactMethods;

		#region Properties

		/// <summary>
		/// Gets the contact id.
		/// </summary>
		public string ContactId { get; private set; }

		/// <summary>
		/// Gets the name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the first name.
		/// </summary>
		public string FirstName { get { return Name.Split().FirstOrDefault(); } }

		/// <summary>
		/// Gets the last name.
		/// </summary>
		public string LastName { get { return Name.Split().LastOrDefault(); } }

		/// <summary>
		/// Gets the folder id.
		/// </summary>
		public string FolderId { get; private set; }

		/// <summary>
		/// Gets the phonebook type.
		/// </summary>
		public ePhonebookType PhonebookType
		{
			get { return (ContactId.Contains("local")) ? ePhonebookType.Local : ePhonebookType.Corporate; }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="contactId"></param>
		/// <param name="folderId"></param>
		/// <param name="contactMethods"></param>
		public CiscoContact(string name, string contactId, string folderId, IEnumerable<IContactMethod> contactMethods)
		{
			Name = name;
			ContactId = contactId;
			FolderId = folderId;
			m_ContactMethods = contactMethods.ToArray();
		}

		/// <summary>
		/// Builds a Contact from an XML Contact Element.
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="idPrefix"></param>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static CiscoContact FromXml(string xml, string idPrefix, IDictionary<string, CiscoContact> cache)
		{
			string contactId = XmlUtils.ReadChildElementContentAsString(xml, "ContactId");
			string cachedId = idPrefix + contactId;

			if (!cache.ContainsKey(cachedId))
			{
				string name = XmlUtils.TryReadChildElementContentAsString(xml, "Name");
				string folderId = XmlUtils.TryReadChildElementContentAsString(xml, "FolderId");

				IEnumerable<IContactMethod> contactMethods = XmlUtils.GetChildElementsAsString(xml, "ContactMethod")
				                                                     .Select(e => CiscoContactMethod.FromXml(e))
				                                                     .Cast<IContactMethod>();

				cache[cachedId] = new CiscoContact(name, contactId, folderId, contactMethods);
			}

			return cache[cachedId];
		}

		#endregion

		/// <summary>
		/// Gets the contact methods.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IContactMethod> GetContactMethods()
		{
			return m_ContactMethods;
		}
	}
}
