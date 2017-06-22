﻿using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp.CrestronData;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.SQLite;
using ICD.Common.Utils.IO;

namespace ICD.Connect.Conferencing.Favorites.SqLite
{
	public sealed class SqLiteFavorites : IFavorites
	{
		public const string SQLITE_EXT = ".sqlite";
		public const string TABLE = "favorites";

		private const string COLUMN_ID = "id";
		private const string COLUMN_NAME = "name";

		private const string PARAM_ID = "@" + COLUMN_ID;
		private const string PARAM_NAME = "@" + COLUMN_NAME;

		private readonly SqLiteFavoriteContactMethods m_ContactMethods;
		private readonly string m_DataPath;

		#region Properties

		/// <summary>
		/// Gets the connection string.
		/// </summary>
		private string ConnectionString { get { return string.Format("Data Source={0};", m_DataPath); } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="path"></param>
		public SqLiteFavorites(string path)
		{
			m_DataPath = IcdPath.ChangeExtension(path, SQLITE_EXT);

			m_ContactMethods = new SqLiteFavoriteContactMethods(m_DataPath);

			if (!File.Exists(m_DataPath))
				SQLiteConnection.CreateFile(m_DataPath);

			CreateTable();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets all of the favorites.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Favorite> GetFavorites()
		{
			string query = string.Format("SELECT * FROM {0}", TABLE);

			using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
			{
				using (SQLiteCommand command = new SQLiteCommand(query, connection))
				{
					connection.Open();

					using (SQLiteDataReader reader = command.ExecuteReader())
						return FavoritesFromReader(reader).ToArray();
				}
			}
		}

		/// <summary>
		/// Gets favorite by id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public Favorite GetFavorite(long id)
		{
			string query = string.Format("SELECT * FROM {0} WHERE {1}={2}", TABLE, COLUMN_ID, PARAM_ID);

			using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
			{
				using (SQLiteCommand command = new SQLiteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_ID, DbType.Int64).Value = id;

					connection.Open();

					using (SQLiteDataReader reader = command.ExecuteReader())
						return FavoritesFromReader(reader).FirstOrDefault();
				}
			}
		}

		/// <summary>
		/// Gets the favorites with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public IEnumerable<Favorite> GetFavoritesByName(string name)
		{
			string query = string.Format("SELECT * FROM {0} WHERE {1}={2}", TABLE, COLUMN_NAME, PARAM_NAME);

			using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
			{
				using (SQLiteCommand command = new SQLiteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_NAME, DbType.String).Value = name;

					connection.Open();

					using (SQLiteDataReader reader = command.ExecuteReader())
						return FavoritesFromReader(reader).ToArray();
				}
			}
		}

		/// <summary>
		/// Gets the favorites with the given contact number.
		/// </summary>
		/// <param name="contactNumber"></param>
		/// <returns></returns>
		public IEnumerable<Favorite> GetFavoritesByContactNumber(string contactNumber)
		{
			string query = string.Format(@"SELECT * FROM {0} INNER JOIN {1} ON {2}.{3}={4}.{5} WHERE {6}={7}",
			                             TABLE, SqLiteFavoriteContactMethods.TABLE, TABLE, COLUMN_ID,
			                             SqLiteFavoriteContactMethods.TABLE,
			                             SqLiteFavoriteContactMethods.COLUMN_FAVORITE_ID,
			                             SqLiteFavoriteContactMethods.COLUMN_NUMBER,
			                             SqLiteFavoriteContactMethods.PARAM_NUMBER);

			using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
			{
				using (SQLiteCommand command = new SQLiteCommand(query, connection))
				{
					command.Parameters.Add(SqLiteFavoriteContactMethods.PARAM_NUMBER, DbType.String).Value = contactNumber;

					connection.Open();

					using (SQLiteDataReader reader = command.ExecuteReader())
						return FavoritesFromReader(reader).ToArray();
				}
			}
		}

		/// <summary>
		/// Adds the source as a favorite. Returns null if the source is already a favorite.
		/// </summary>
		/// <param name="favorite"></param>
		/// <returns></returns>
		public Favorite SubmitFavorite(Favorite favorite)
		{
			// Add the favorite
			string query = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", TABLE, COLUMN_NAME, PARAM_NAME);

			bool inserted;
			long lastId = 0;

			using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
			{
				connection.Open();

				using (SQLiteCommand command = new SQLiteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_NAME, DbType.String).Value = favorite.Name;
					inserted = command.ExecuteNonQuery() == 1;
				}

				// Retrieval
				if (inserted)
				{
					using (SQLiteCommand command = new SQLiteCommand("select last_insert_rowid()", connection))
						lastId = (long)command.ExecuteScalar();
				}
			}

			if (!inserted)
				return null;

			// Add all of the contact methods
			foreach (FavoriteContactMethod contactMethod in favorite.GetContactMethods())
				m_ContactMethods.SubmitContactMethod(lastId, contactMethod);

			return GetFavorite(lastId);
		}

		/// <summary>
		/// Updates the existing favorite.
		/// </summary>
		/// <param name="favorite"></param>
		/// <returns>Null if the favorite does not exist.</returns>
		public Favorite UpdateFavorite(Favorite favorite)
		{
			// Add all of the contact methods
			foreach (FavoriteContactMethod contactMethod in favorite.GetContactMethods())
				m_ContactMethods.SubmitContactMethod(favorite.Id, contactMethod);

			string query = string.Format("UPDATE {0} SET {1}={2} WHERE {3}={4}", TABLE, COLUMN_NAME, PARAM_NAME, COLUMN_ID,
			                             PARAM_ID);

			using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
			{
				using (SQLiteCommand command = new SQLiteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_ID, DbType.Int64).Value = favorite.Id;
					command.Parameters.Add(PARAM_NAME, DbType.String).Value = favorite.Name;

					connection.Open();
					if (command.ExecuteNonQuery() != 1)
						return null;
				}
			}

			return GetFavorite(favorite.Id);
		}

		/// <summary>
		/// Removes the favorite.
		/// </summary>
		/// <param name="favorite"></param>
		/// <returns>False if the favorite does not exist.</returns>
		public bool RemoveFavorite(Favorite favorite)
		{
			// Remove contact methods
			foreach (FavoriteContactMethod method in m_ContactMethods.GetContactMethodsForFavorite(favorite.Id))
				m_ContactMethods.RemoveContactMethod(method);

			string query = string.Format("DELETE FROM {0} WHERE {1}={2}", TABLE, COLUMN_ID, PARAM_ID);

			// Remove the favorite
			using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
			{
				using (SQLiteCommand command = new SQLiteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_ID, DbType.Int64).Value = favorite.Id;

					connection.Open();
					return command.ExecuteNonQuery() == 1;
				}
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the favorites from the reader.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		private static IEnumerable<Favorite> FavoritesFromReader(IDataReader reader)
		{
			while (reader.Read())
				yield return RowToFavorite(reader);
		}

		/// <summary>
		/// Gets a Favorite from a row.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		private static Favorite RowToFavorite(IDataRecord reader)
		{
			return new Favorite
			{
				Id = (long)reader[COLUMN_ID],
				Name = reader[COLUMN_NAME] as string,
			};
		}

		/// <summary>
		/// Creates the favorites table.
		/// </summary>
		private void CreateTable()
		{
			string query =
				string.Format("CREATE TABLE IF NOT EXISTS {0} ({1} INTEGER PRIMARY KEY, {2} VARCHAR(40) UNIQUE NOT NULL)",
				              TABLE, COLUMN_ID, COLUMN_NAME);

			using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
			{
				using (SQLiteCommand command = new SQLiteCommand(query, connection))
				{
					connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}

		#endregion
	}
}
