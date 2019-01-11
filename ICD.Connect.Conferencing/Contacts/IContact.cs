using System.Collections.Generic;
using ICD.Connect.Conferencing.DialContexts;

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
		IEnumerable<IDialContext> GetDialContexts();
	}
}
