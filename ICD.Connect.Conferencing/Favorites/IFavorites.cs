using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialContexts;

namespace ICD.Connect.Conferencing.Favorites
{
	public interface IFavorites
	{
		/// <summary>
		/// Gets all of the favorites.
		/// </summary>
		/// <returns></returns>
		IEnumerable<Favorite> GetFavorites();

		/// <summary>
		/// Gets favorite by id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		Favorite GetFavorite(long id);

		/// <summary>
		/// Gets the favorites with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[NotNull]
		IEnumerable<Favorite> GetFavoritesByName([NotNull] string name);

		/// <summary>
		/// Gets the favorites with the given dialContext.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		[NotNull]
		IEnumerable<Favorite> GetFavoritesByDialContext([NotNull] IDialContext dialContext);

		/// <summary>
		/// Gets the favorites with the given protocol.
		/// </summary>
		/// <param name="protocol"></param>
		/// <returns></returns>
		[NotNull]
		IEnumerable<Favorite> GetFavoritesByProtocol(eDialProtocol protocol);

		/// <summary>
		/// Adds the favorite. Returns null if a favorite with the same id already exists.
		/// </summary>
		/// <param name="favorite"></param>
		/// <returns></returns>
		Favorite SubmitFavorite(Favorite favorite);

		/// <summary>
		/// Updates the existing favorite.
		/// </summary>
		/// <param name="favorite"></param>
		/// <returns>False if the favorite does not exist.</returns>
		Favorite UpdateFavorite(Favorite favorite);

		/// <summary>
		/// Removes the favorite.
		/// </summary>
		/// <param name="favorite"></param>
		/// <returns>False if the favorite does not exist.</returns>
		bool RemoveFavorite([NotNull] Favorite favorite);
	}

	public static class FavoritesExtensions
	{
		/// <summary>
		/// Returns true if the contact is stored as a favorite.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		/// <returns></returns>
		[PublicAPI]
		public static bool ContainsFavorite([NotNull] this IFavorites extends, [NotNull] IContact contact)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (contact == null)
				throw new ArgumentNullException("contact");

			return extends.GetFavorite(contact) != null;
		}

		/// <summary>
		/// Returns true if the contact was stored as a favorite and successfully removed.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		/// <returns></returns>
		[PublicAPI]
		public static bool RemoveFavorite([NotNull] this IFavorites extends, [NotNull] IContact contact)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (contact == null)
				throw new ArgumentNullException("contact");

			Favorite favorite = extends.GetFavorite(contact);
			return favorite != null && extends.RemoveFavorite(favorite);
		}

		/// <summary>
		/// Stores the contact as a favorite.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		/// <returns></returns>
		[PublicAPI]
		[NotNull]
		public static Favorite SubmitFavorite([NotNull] this IFavorites extends, [NotNull] IContact contact)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (contact == null)
				throw new ArgumentNullException("contact");

			Favorite favorite = Favorite.FromContact(contact);
			return extends.SubmitFavorite(favorite);
		}

		/// <summary>
		/// Gets the favorite for the given favorite.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		/// <returns></returns>
		[PublicAPI]
		[CanBeNull]
		public static Favorite GetFavorite([NotNull] this IFavorites extends, [NotNull] IContact contact)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (contact == null)
				throw new ArgumentNullException("contact");

			// First try to find the favorite by looking up the contact number
			Favorite output = contact.GetDialContexts()
			                         .SelectMany(d => extends.GetFavoritesByDialContext(d))
			                         .FirstOrDefault();

			// Then try to find the favorite by name
			return output ?? extends.GetFavoritesByName(contact.Name).FirstOrDefault();
		}

		/// <summary>
		/// If the contact is already stored as a favorite remove the favorite,
		/// otherwise add the contact as a new favorite.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		/// <returns>True if the contact is stored as a favorite, false if removed.</returns>
		public static bool ToggleFavorite([NotNull] this IFavorites extends, [NotNull] IContact contact)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (contact == null)
				throw new ArgumentNullException("contact");

			if (extends.ContainsFavorite(contact))
			{
				extends.RemoveFavorite(contact);
				return false;
			}

			extends.SubmitFavorite(contact);
			return true;
		}
	}
}
