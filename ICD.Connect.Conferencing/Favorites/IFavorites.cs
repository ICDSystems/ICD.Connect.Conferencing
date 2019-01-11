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
		IEnumerable<Favorite> GetFavoritesByName(string name);

		/// <summary>
		/// Gets the favorites with the given dialContext.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		IEnumerable<Favorite> GetFavoritesByDialContext(IDialContext dialContext);

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
		bool RemoveFavorite(Favorite favorite);
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
		public static bool ContainsFavorite(this IFavorites extends, IContact contact)
		{
			return extends.GetFavorite(contact) != null;
		}

		/// <summary>
		/// Returns true if the contact was stored as a favorite and successfully removed.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		/// <returns></returns>
		[PublicAPI]
		public static bool RemoveFavorite(this IFavorites extends, IContact contact)
		{
			Favorite favorite = extends.GetFavorite(contact);
			return favorite != null && extends.RemoveFavorite(favorite);
		}

		/// <summary>
		/// Returns true if the contact was successfully stored as a favorite.
		/// </summary>
		/// <param name="extends"></param>
		/// <param name="contact"></param>
		/// <returns></returns>
		[PublicAPI]
		public static Favorite SubmitFavorite(this IFavorites extends, IContact contact)
		{
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
		public static Favorite GetFavorite(this IFavorites extends, IContact contact)
		{
			// First try to find the favorite by looking up the contact number
			Favorite output = contact.GetDialContexts()
			                         .SelectMany(d => extends.GetFavoritesByDialContext(d))
			                         .FirstOrDefault();

			// Then try to find the favorite by name
			return output ?? extends.GetFavoritesByName(contact.Name).FirstOrDefault();
		}
	}
}
