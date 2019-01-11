using System.Collections.Generic;
using System.Linq;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialContexts;

namespace ICD.Connect.Conferencing.Favorites
{
	/// <summary>
	/// A Favorite represents a conferencing source that is being stored for future use.
	/// </summary>
	public sealed class Favorite : IContact
	{
		private FavoriteDialContext[] m_DialContexts;

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
			m_DialContexts = new FavoriteDialContext[0];
		}

		/// <summary>
		/// Instantiates a favorite from the given contact.
		/// </summary>
		/// <param name="contact"></param>
		/// <returns></returns>
		public static Favorite FromContact(IContact contact)
		{
			Favorite output = new Favorite {Name = contact.Name};
			output.SetContactMethods(contact.GetDialContexts()
				.Select(m => FavoriteDialContext.FromDialContext(m)).Where(f => f != null));
			return output;
		}

		#endregion

		/// <summary>
		/// Sets the contact methods.
		/// </summary>
		/// <param name="contactMethods"></param>
		public void SetContactMethods(IEnumerable<FavoriteDialContext> contactMethods)
		{
			m_DialContexts = contactMethods.ToArray();
		}

		/// <summary>
		/// Gets the contact methods.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IDialContext> GetDialContexts()
		{
			return m_DialContexts;
		}

		public IEnumerable<FavoriteDialContext> GetContactMethods()
		{
			return m_DialContexts;
		} 
	}
}
