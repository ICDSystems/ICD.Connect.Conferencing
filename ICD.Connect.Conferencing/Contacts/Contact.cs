using System.Collections.Generic;
using ICD.Connect.Conferencing.DialContexts;

namespace ICD.Connect.Conferencing.Contacts
{
	public sealed class Contact : IContact
	{
		private readonly IDialContext[] m_DialContexts;
		private readonly string m_Name;

		/// <summary>
		/// Gets the contact name.
		/// </summary>
		public string Name { get { return m_Name; } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="dialContexts"></param>
		public Contact(string name, IDialContext[] dialContexts)
		{
			m_Name = name;
			m_DialContexts = dialContexts;
		}

		/// <summary>
		/// Gets the contact methods.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IDialContext> GetDialContexts()
		{
			return m_DialContexts;
		}
	}
}
