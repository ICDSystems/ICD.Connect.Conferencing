using System;
using System.Collections.Generic;
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
		[NotNull]
		IEnumerable<Favorite> GetFavorites();

		/// <summary>
		/// Gets the favorites with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		[NotNull]
		Favorite GetFavorite([NotNull] string name);

		/// <summary>
		/// Gets the favorites with the given protocol.
		/// </summary>
		/// <param name="protocol"></param>
		/// <returns></returns>
		[NotNull]
		IEnumerable<Favorite> GetFavorites(eDialProtocol protocol);

		/// <summary>
		/// Returns true if there is a stored favorite with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		bool ContainsFavorite(string name);

		/// <summary>
		/// Adds the favorite.
		/// </summary>
		/// <param name="favorite"></param>
		/// <returns></returns>
		[NotNull]
		Favorite SubmitFavorite([NotNull] Favorite favorite);

		/// <summary>
		/// Removes the favorite with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		bool RemoveFavorite([NotNull] string name);

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

			return extends.ContainsFavorite(contact.Name);
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

			return extends.RemoveFavorite(contact.Name);
		}

		/// <summary>
		/// Stores the contact as a favorite.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		/// <returns></returns>
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
		[NotNull]
		public static Favorite GetFavorite([NotNull] this IFavorites extends, [NotNull] IContact contact)
		{
			if (extends == null)
				throw new ArgumentNullException("extends");

			if (contact == null)
				throw new ArgumentNullException("contact");

			return extends.GetFavorite(contact.Name);
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

			if (extends.RemoveFavorite(contact))
				return false;

			extends.SubmitFavorite(contact);
			return true;
		}
	}
}
