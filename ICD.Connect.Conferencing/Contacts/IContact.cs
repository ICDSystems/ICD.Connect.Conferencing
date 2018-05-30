using System.Collections.Generic;

namespace ICD.Connect.Conferencing.Contacts
{
	public interface IContact
	{
		/// <summary>
		/// Gets the contact name.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Gets the contact methods.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IContactMethod> GetContactMethods();
	}
}
