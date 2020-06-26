using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Settings.ORM;

namespace ICD.Connect.Conferencing.Favorites
{
	/// <summary>
	/// A Favorite represents a conferencing source that is being stored for future use.
	/// </summary>
	public sealed class Favorite : IContact
	{
		/// <summary>
		/// Raised when the favorites for a given room change.
		/// </summary>
		public static event EventHandler<IntEventArgs> OnFavoritesChanged;

		#region Properties

		[PrimaryKey]
		[Obfuscation(Exclude = true)]
		public int Id { get; set; }

		/// <summary>
		/// Gets/sets the name.
		/// </summary>
		[DataField]
		[Obfuscation(Exclude = true)]
		public string Name { get; set; }

		[ForeignKey]
		[Obfuscation(Exclude = true)]
		public IEnumerable<FavoriteDialContext> DialContexts { get; set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public Favorite()
		{
			DialContexts = new FavoriteDialContext[0];
		}

		/// <summary>
		/// Instantiates a favorite from the given contact.
		/// </summary>
		/// <param name="contact"></param>
		/// <returns></returns>
		public static Favorite FromContact(IContact contact)
		{
			return new Favorite
			{
				Name = contact.Name,
				DialContexts = contact.GetDialContexts()
				                      .Select(m => FavoriteDialContext.FromDialContext(m))
				                      .ToArray()
			};
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the contact methods.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IDialContext> GetDialContexts()
		{
			return DialContexts == null
				? Enumerable.Empty<IDialContext>()
				: DialContexts.Cast<IDialContext>();
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Returns true if there is a favorite for the given contact.
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="contact"></param>
		/// <returns></returns>
		public static bool Contains(int roomId, [NotNull] IContact contact)
		{
			return Persistent.Db(eDb.RoomPreferences, roomId.ToString()).Get<Favorite>(new {contact.Name}) != null;
		}

		/// <summary>
		/// Returns all of the favorites stored in the given database.
		/// </summary>
		/// <param name="roomId"></param>
		/// <returns></returns>
		public static IEnumerable<Favorite> All(int roomId)
		{
			return Persistent.Db(eDb.RoomPreferences, roomId.ToString()).All<Favorite>();
		}

		/// <summary>
		/// Removes the contact from the given favorite database.
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="contact"></param>
		public static void Delete(int roomId, [NotNull] IContact contact)
		{
			Persistent.Db(eDb.RoomPreferences, roomId.ToString()).Delete<Favorite>(new {contact.Name});
			
			OnFavoritesChanged.Raise(null, new IntEventArgs(roomId));
		}

		/// <summary>
		/// Adds the contact to the given database.
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="contact"></param>
		public static void Insert(int roomId, [NotNull] IContact contact)
		{
			Favorite favorite = contact as Favorite ?? FromContact(contact);
			Persistent.Db(eDb.RoomPreferences, roomId.ToString()).Insert<Favorite>(favorite);

			OnFavoritesChanged.Raise(null, new IntEventArgs(roomId));
		}

		/// <summary>
		/// Updates the given favorite in the database.
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="favorite"></param>
		public static void Update(int roomId, Favorite favorite)
		{
			Persistent.Db(eDb.RoomPreferences, roomId.ToString()).Update<Favorite>(favorite);

			OnFavoritesChanged.Raise(null, new IntEventArgs(roomId));
		}

		/// <summary>
		/// Unfavorites the given contact, otherwise favorites it.
		/// </summary>
		/// <param name="roomId"></param>
		/// <param name="contact"></param>
		public static void Toggle(int roomId, [NotNull] IContact contact)
		{
			if (Contains(roomId, contact))
				Delete(roomId, contact);
			else 
				Insert(roomId, contact);
		}

		#endregion
	}
}
