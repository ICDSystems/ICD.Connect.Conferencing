using System.Collections.Generic;

namespace ICD.Connect.Conferencing.Contacts
{
	public sealed class Contact : IContact
	{
		private readonly IContactMethod[] m_ContactMethods;
		private readonly string m_Name;

		/// <summary>
		/// Gets the contact name.
		/// </summary>
		public string Name { get { return m_Name; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="contactMethods"></param>
		public Contact(string name, IContactMethod[] contactMethods)
		{
			m_Name = name;
			m_ContactMethods = contactMethods;
		}

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
