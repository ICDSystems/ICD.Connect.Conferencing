using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Sqlite;
using ICD.Connect.Conferencing.DialContexts;

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

		/// <summary>
		/// Gets the connection string.
		/// </summary>
		private string ConnectionString { get { return string.Format("Data Source={0};", m_DataPath); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="path"></param>
		public SqLiteFavorites(string path)
		{
			m_DataPath = IcdPath.ChangeExtension(path, SQLITE_EXT);

			m_ContactMethods = new SqLiteFavoriteContactMethods(m_DataPath);

			if (!IcdFile.Exists(m_DataPath))
				IcdSqliteConnection.CreateFile(m_DataPath);

			CreateTable();
		}

		#region Methods

		/// <summary>
		/// Gets all of the favorites.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<Favorite> GetFavorites()
		{
			string query = string.Format("SELECT {0}.{1}, {0}.{2} FROM {0}", TABLE, COLUMN_ID, COLUMN_NAME);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					connection.Open();

					using (IcdSqliteDataReader reader = command.ExecuteReader())
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
			string query = string.Format("SELECT {0}.{1}, {0}.{2} FROM {0} WHERE {0}.{3}={4} LIMIT 1", TABLE, COLUMN_ID, COLUMN_NAME,
			                             COLUMN_ID, PARAM_ID);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_ID, eDbType.Int64).Value = id;

					connection.Open();

					using (IcdSqliteDataReader reader = command.ExecuteReader())
						return FavoritesFromReader(reader).First();
				}
			}
		}

		/// <summary>
		/// Gets the favorites with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public Favorite GetFavorite(string name)
		{
			string query = string.Format("SELECT {0}.{1}, {0}.{2} FROM {0} WHERE {0}.{3}={4} LIMIT 1", TABLE, COLUMN_ID, COLUMN_NAME,
			                             COLUMN_NAME, PARAM_NAME);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_NAME, eDbType.String).Value = name;

					connection.Open();

					using (IcdSqliteDataReader reader = command.ExecuteReader())
						return FavoritesFromReader(reader).First();
				}
			}
		}

		/// <summary>
		/// Gets the favorites with the given protocol.
		/// </summary>
		/// <param name="protocol"></param>
		/// <returns></returns>
		public IEnumerable<Favorite> GetFavorites(eDialProtocol protocol)
		{
			string query = string.Format(@"SELECT {0}.{1}, {0}.{2} FROM {0} INNER JOIN {3} ON {0}.{1}={3}.{4} WHERE {5}={6}",
										 TABLE, COLUMN_ID, COLUMN_NAME,
			                             SqLiteFavoriteContactMethods.TABLE,
			                             SqLiteFavoriteContactMethods.COLUMN_FAVORITE_ID,
			                             SqLiteFavoriteContactMethods.COLUMN_DIAL_PROTOCOL,
			                             SqLiteFavoriteContactMethods.PARAM_DIAL_PROTOCOL);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(SqLiteFavoriteContactMethods.PARAM_DIAL_PROTOCOL, eDbType.Int32).Value = (int)protocol;

					connection.Open();

					using (IcdSqliteDataReader reader = command.ExecuteReader())
						return FavoritesFromReader(reader).ToArray();
				}
			}
		}

		/// <summary>
		/// Returns true if there is a stored favorite with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool ContainsFavorite(string name)
		{
			string query = string.Format("SELECT 1 FROM {0} WHERE {0}.{1}={2} LIMIT 1", TABLE,
			                             COLUMN_NAME, PARAM_NAME);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_NAME, eDbType.String).Value = name;

					connection.Open();

					return command.ExecuteScalar() != null;
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
			string query = string.Format("INSERT INTO {0} ({1}) VALUES ({2})", TABLE, COLUMN_NAME, PARAM_NAME);

			long lastId;

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				connection.Open();

				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_NAME, eDbType.String).Value = favorite.Name;
					if (command.ExecuteNonQuery() != 1)
						throw new InvalidOperationException("Failed to insert new favorite");
				}

				using (IcdSqliteCommand command = new IcdSqliteCommand("select last_insert_rowid()", connection))
					lastId = (long)command.ExecuteScalar();
			}

			// Add all of the contact methods
			m_ContactMethods.SubmitContactMethods(lastId, favorite.GetContactMethods());

			return GetFavorite(lastId);
		}

		/// <summary>
		/// Removes the favorite with the given name.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool RemoveFavorite(string name)
		{
			// Get the id
			string query = string.Format("SELECT {0}.{1} FROM {0} WHERE {0}.{2}={3} LIMIT 1", TABLE, COLUMN_ID,
			                             COLUMN_NAME, PARAM_NAME);

			long id;

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_NAME, eDbType.String).Value = name;

					connection.Open();

					object result = command.ExecuteScalar();
					if (result == null)
						return false;

					id = (long)result;
				}
			}

			return RemoveFavorite(id);
		}

		/// <summary>
		/// Removes the favorite.
		/// </summary>
		/// <param name="favorite"></param>
		/// <returns>False if the favorite does not exist.</returns>
		public bool RemoveFavorite(Favorite favorite)
		{
			if (favorite == null)
				throw new ArgumentNullException("favorite");

			return RemoveFavorite(favorite.Id);
		}

		/// <summary>
		/// Removes the favorite with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns>False if the favorite does not exist.</returns>
		private bool RemoveFavorite(long id)
		{
			// Remove contact methods
			m_ContactMethods.RemoveContactMethodsForFavorite(id);

			string query = string.Format("DELETE FROM {0} WHERE {0}.{1}={2}", TABLE, COLUMN_ID, PARAM_ID);

			// Remove the favorite
			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_ID, eDbType.Int64).Value = id;

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
		private IEnumerable<Favorite> FavoritesFromReader(IIcdDataReader reader)
		{
			while (reader.Read())
				yield return RowToFavorite(reader);
		}

		/// <summary>
		/// Gets a Favorite from a row.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		private Favorite RowToFavorite(IIcdDataRecord reader)
		{
			Favorite output = new Favorite
			{
				Id = (long)reader[COLUMN_ID],
				Name = reader[COLUMN_NAME] as string,
			};

			IEnumerable<FavoriteDialContext> contactMethods =
				m_ContactMethods.GetContactMethodsForFavorite(output.Id);

			output.SetContactMethods(contactMethods);

			return output;
		}

		/// <summary>
		/// Creates the favorites table.
		/// </summary>
		private void CreateTable()
		{
			string query =
				string.Format("CREATE TABLE IF NOT EXISTS {0} ({1} INTEGER PRIMARY KEY, {2} VARCHAR(40) UNIQUE NOT NULL)",
				              TABLE, COLUMN_ID, COLUMN_NAME);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					connection.Open();
					command.ExecuteNonQuery();
				}
			}
		}

		#endregion
	}
}
