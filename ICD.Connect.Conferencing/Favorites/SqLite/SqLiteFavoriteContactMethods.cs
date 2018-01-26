using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Sqlite;

namespace ICD.Connect.Conferencing.Favorites.SqLite
{
	public sealed class SqLiteFavoriteContactMethods
	{
		private const string SQLITE_EXT = ".sqlite";
		public const string TABLE = "favorite_contact_methods";

		private const string COLUMN_ID = "id";
		public const string COLUMN_FAVORITE_ID = "favorite_id";
		public const string COLUMN_NUMBER = "number";

		private const string PARAM_ID = "@" + COLUMN_ID;
		private const string PARAM_FAVORITE_ID = "@" + COLUMN_FAVORITE_ID;
		public const string PARAM_NUMBER = "@" + COLUMN_NUMBER;

		#region Properties

		private readonly string m_DataPath;

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
		public SqLiteFavoriteContactMethods(string path)
		{
			m_DataPath = IcdPath.ChangeExtension(path, SQLITE_EXT);

			if (!IcdFile.Exists(m_DataPath))
				IcdSqliteConnection.CreateFile(m_DataPath);

			CreateTable();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets all of the stored contact methods.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<FavoriteContactMethod> GetContactMethods()
		{
			string query = string.Format("SELECT * FROM {0}", TABLE);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					connection.Open();
					using (IcdSqliteDataReader reader = command.ExecuteReader())
						return ContactMethodsFromReader(reader).ToArray();
				}
			}
		}

		/// <summary>
		/// Gets the contact method with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[PublicAPI]
		public FavoriteContactMethod GetContactMethod(long id)
		{
			string query = string.Format("SELECT * FROM {0} WHERE {1}={2}", TABLE, COLUMN_ID, PARAM_ID);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_ID, eDbType.Int64).Value = id;

					connection.Open();
					using (IcdSqliteDataReader reader = command.ExecuteReader())
						return ContactMethodsFromReader(reader).FirstOrDefault();
				}
			}
		}

		/// <summary>
		/// Gets contact methods for the given favorite id.
		/// </summary>
		/// <param name="favoriteId"></param>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<FavoriteContactMethod> GetContactMethodsForFavorite(long favoriteId)
		{
			string query = string.Format("SELECT * FROM {0} WHERE {1}={2}", TABLE, COLUMN_FAVORITE_ID, PARAM_FAVORITE_ID);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_FAVORITE_ID, eDbType.Int64).Value = favoriteId;

					connection.Open();
					using (IcdSqliteDataReader reader = command.ExecuteReader())
						return ContactMethodsFromReader(reader).ToArray();
				}
			}
		}

		/// <summary>
		/// Gets contact method for the given number.
		/// </summary>
		/// <param name="number"></param>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<FavoriteContactMethod> GetContactMethodsByNumber(string number)
		{
			string query = string.Format("SELECT * FROM {0} WHERE {1}={2}", TABLE, COLUMN_NUMBER, PARAM_NUMBER);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_NUMBER, eDbType.String).Value = number;

					connection.Open();
					using (IcdSqliteDataReader reader = command.ExecuteReader())
						return ContactMethodsFromReader(reader).ToArray();
				}
			}
		}

		/// <summary>
		/// Stores the contact method for the given parent favorite. Returns false if a contact
		/// method already exists with the same id.
		/// </summary>
		/// <param name="favoriteId"></param>
		/// <param name="contactMethod"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool SubmitContactMethod(long favoriteId, FavoriteContactMethod contactMethod)
		{
			// Insertion
			string query =
				string.Format("INSERT INTO {0} ({1}, {2}, {3}) VALUES ({4}, {5}, {6})",
				              TABLE, COLUMN_ID, COLUMN_FAVORITE_ID, COLUMN_NUMBER, PARAM_ID, PARAM_FAVORITE_ID, PARAM_NUMBER);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_ID, eDbType.Int64).Value = contactMethod.Id;
					command.Parameters.Add(PARAM_FAVORITE_ID, eDbType.Int64).Value = favoriteId;
					command.Parameters.Add(PARAM_NUMBER, eDbType.String).Value = contactMethod.Number;

					connection.Open();
					return command.ExecuteNonQuery() == 1;
				}
			}
		}

		/// <summary>
		/// Removes the contact method.
		/// </summary>
		/// <param name="contactMethod"></param>
		/// <returns>False if the contact method does not exist.</returns>
		[PublicAPI]
		public bool RemoveContactMethod(FavoriteContactMethod contactMethod)
		{
			string query = string.Format("DELETE FROM {0} WHERE {1}={2}", TABLE, COLUMN_ID, PARAM_ID);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_ID, eDbType.Int64).Value = contactMethod.Id;

					connection.Open();
					return command.ExecuteNonQuery() == 1;
				}
			}
		}

		/// <summary>
		/// Updates the contact method with the given id.
		/// </summary>
		/// <param name="contactMethod"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool UpdateContactMethod(FavoriteContactMethod contactMethod)
		{
			string query = string.Format("UPDATE {0} SET {1}={2} WHERE {3}={4}", TABLE, COLUMN_NUMBER, PARAM_NUMBER, COLUMN_ID,
			                             PARAM_ID);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_NUMBER, eDbType.String).Value = contactMethod.Number;

					connection.Open();
					return command.ExecuteNonQuery() == 1;
				}
			}
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets the contact methods from the reader.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		private static IEnumerable<FavoriteContactMethod> ContactMethodsFromReader(IDataReader reader)
		{
			while (reader.Read())
				yield return RowToContactMethod(reader);
		}

		/// <summary>
		/// Gets a contact method from a row.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		private static FavoriteContactMethod RowToContactMethod(IDataRecord reader)
		{
			return new FavoriteContactMethod
			{
				Id = (long)reader[COLUMN_ID],
				Number = reader[COLUMN_NUMBER] as string,
			};
		}

		/// <summary>
		/// Creates the contact methods table.
		/// </summary>
		private void CreateTable()
		{
			string favoriteId = string.Format("{0} INTEGER, FOREIGN KEY ({0}) REFERENCES {1}({2})", COLUMN_FAVORITE_ID,
			                                  SqLiteFavorites.TABLE, COLUMN_ID);
			string query =
				string.Format("CREATE TABLE IF NOT EXISTS {0} ({1} INTEGER PRIMARY KEY, {2} VARCHAR(40) NOT NULL, {3})",
				              TABLE, COLUMN_ID, COLUMN_NUMBER, favoriteId);

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
