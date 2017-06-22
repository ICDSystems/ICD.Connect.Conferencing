using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.Favorites
{
	/// <summary>
	/// A Favorite represents a conferencing source that is being stored for future use.
	/// </summary>
	public sealed class Favorite : IContact
	{
		private FavoriteContactMethod[] m_ContactMethods;

		#region Properties

		public long Id { get; set; }

		/// <summary>
		/// Gets/sets the name.
		/// </summary>
		public string Name { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public Favorite()
		{
			m_ContactMethods = new FavoriteContactMethod[0];
		}

		/// <summary>
		/// Instantiates a favorite from the given contact.
		/// </summary>
		/// <param name="contact"></param>
		/// <returns></returns>
		public static Favorite FromContact(IContact contact)
		{
			Favorite output = new Favorite {Name = contact.Name};
			output.SetContactMethods(contact.GetContactMethods().Select(m => FavoriteContactMethod.FromContactMethod(m)));
			return output;
		}

		#endregion

		/// <summary>
		/// Sets the contact methods.
		/// </summary>
		/// <param name="contactMethods"></param>
		private void SetContactMethods(IEnumerable<FavoriteContactMethod> contactMethods)
		{
			m_ContactMethods = contactMethods.ToArray();
		}

		/// <summary>
		/// Gets the contact methods.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<FavoriteContactMethod> GetContactMethods()
		{
			return m_ContactMethods;
		}

		IEnumerable<IContactMethod> IContact.GetContactMethods()
		{
			return GetContactMethods().Cast<IContactMethod>();
		}
	}
}
