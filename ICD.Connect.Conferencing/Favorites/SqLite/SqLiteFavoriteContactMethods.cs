using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.IO;
using ICD.Common.Utils.Sqlite;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Favorites.SqLite
{
	public sealed class SqLiteFavoriteContactMethods
	{
		private const string SQLITE_EXT = ".sqlite";
		public const string TABLE = "favorite_contact_methods";

		private const string COLUMN_ID = "id";
		public const string COLUMN_FAVORITE_ID = "favorite_id";
		public const string COLUMN_DIAL_STRING = "number";
		public const string COLUMN_DIAL_PROTOCOL = "dial_protocol";
		public const string COLUMN_CALL_TYPE = "call_type";

		private const string PARAM_ID = "@" + COLUMN_ID;
		private const string PARAM_FAVORITE_ID = "@" + COLUMN_FAVORITE_ID;
		public const string PARAM_DIAL_STRING = "@" + COLUMN_DIAL_STRING;
		public const string PARAM_DIAL_PROTOCOL = "@" + COLUMN_DIAL_PROTOCOL;
		public const string PARAM_CALL_TYPE = "@" + COLUMN_CALL_TYPE;

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
		public IEnumerable<FavoriteDialContext> GetContactMethods()
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
		public FavoriteDialContext GetContactMethod(long id)
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
		public IEnumerable<FavoriteDialContext> GetContactMethodsForFavorite(long favoriteId)
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
		public IEnumerable<FavoriteDialContext> GetContactMethodsByNumber(string number)
		{
			string query = string.Format("SELECT * FROM {0} WHERE {1}={2}", TABLE, COLUMN_DIAL_STRING, PARAM_DIAL_STRING);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_DIAL_STRING, eDbType.String).Value = number;

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
		/// <param name="dialContext"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool SubmitContactMethod(long favoriteId, FavoriteDialContext dialContext)
		{
			// Insertion
			string query =
				string.Format("INSERT INTO {0} ({1}, {2}, {3}, {4}) VALUES ({5}, {6}, {7}, {8})",
				              TABLE, COLUMN_FAVORITE_ID, COLUMN_DIAL_STRING, COLUMN_DIAL_PROTOCOL, COLUMN_CALL_TYPE,
							  PARAM_FAVORITE_ID, PARAM_DIAL_STRING, PARAM_DIAL_PROTOCOL, PARAM_CALL_TYPE);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_FAVORITE_ID, eDbType.Int64).Value = favoriteId;
					command.Parameters.Add(PARAM_DIAL_STRING, eDbType.String).Value = dialContext.DialString;
					command.Parameters.Add(PARAM_DIAL_STRING, eDbType.Int32).Value = dialContext.Protocol;
					command.Parameters.Add(PARAM_DIAL_STRING, eDbType.Int32).Value = dialContext.CallType;

					connection.Open();
					return command.ExecuteNonQuery() == 1;
				}
			}
		}

		/// <summary>
		/// Removes the contact method.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns>False if the contact method does not exist.</returns>
		[PublicAPI]
		public bool RemoveContactMethod(FavoriteDialContext dialContext)
		{
			string query = string.Format("DELETE FROM {0} WHERE {1}={2}", TABLE, COLUMN_ID, PARAM_ID);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_ID, eDbType.Int64).Value = dialContext.Id;

					connection.Open();
					return command.ExecuteNonQuery() == 1;
				}
			}
		}

		/// <summary>
		/// Updates the contact method with the given id.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		[PublicAPI]
		public bool UpdateContactMethod(FavoriteDialContext dialContext)
		{
			string query = string.Format("UPDATE {0} SET {1}={2}, {3}={4}, {5}={6} WHERE {7}={8}", TABLE, 
				COLUMN_DIAL_STRING, PARAM_DIAL_STRING,
				COLUMN_DIAL_PROTOCOL, PARAM_DIAL_PROTOCOL,
				COLUMN_CALL_TYPE, PARAM_CALL_TYPE,
				COLUMN_ID, PARAM_ID);

			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				using (IcdSqliteCommand command = new IcdSqliteCommand(query, connection))
				{
					command.Parameters.Add(PARAM_DIAL_STRING, eDbType.String).Value = dialContext.DialString;
					command.Parameters.Add(PARAM_DIAL_PROTOCOL, eDbType.Int32).Value = dialContext.Protocol;
					command.Parameters.Add(PARAM_CALL_TYPE, eDbType.Int32).Value = dialContext.CallType;

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
		private static IEnumerable<FavoriteDialContext> ContactMethodsFromReader(IIcdDataReader reader)
		{
			while (reader.Read())
				yield return RowToContactMethod(reader);
		}

		/// <summary>
		/// Gets a contact method from a row.
		/// </summary>
		/// <param name="reader"></param>
		/// <returns></returns>
		private static FavoriteDialContext RowToContactMethod(IIcdDataRecord reader)
		{
			return new FavoriteDialContext
			{
				Id = (long)reader[COLUMN_ID],
				DialString = reader[COLUMN_DIAL_STRING] as string,
				Protocol = (eDialProtocol)reader[COLUMN_DIAL_PROTOCOL],
				CallType = (eCallType)reader[COLUMN_CALL_TYPE]
			};
		}

		/// <summary>
		/// Creates the contact methods table.
		/// </summary>
		private void CreateTable()
		{
			using (IcdSqliteConnection connection = new IcdSqliteConnection(ConnectionString))
			{
				connection.Open();

				string favoriteId = string.Format("{0} INTEGER, FOREIGN KEY ({0}) REFERENCES {1}({2})", COLUMN_FAVORITE_ID,
			                                  SqLiteFavorites.TABLE, COLUMN_ID);
				string createQuery =
					string.Format("CREATE TABLE IF NOT EXISTS {0} ({1} INTEGER PRIMARY KEY, {2} VARCHAR(40) NOT NULL, {3} INTEGER DEFAULT 1, {4} INTEGER DEFAULT 0, {5})",
								TABLE, COLUMN_ID, COLUMN_DIAL_STRING, COLUMN_DIAL_PROTOCOL, COLUMN_CALL_TYPE, favoriteId);
				using (IcdSqliteCommand command = new IcdSqliteCommand(createQuery, connection))
					command.ExecuteNonQuery();

				bool migrate = true;
				string pragmaQuery = string.Format("PRAGMA table_info({0}", TABLE);
				using (IcdSqliteCommand command = new IcdSqliteCommand(pragmaQuery, connection))
				{
					var result = command.ExecuteReader();

					while (result.Read())
					{
						if (result["name"].Equals(COLUMN_DIAL_PROTOCOL) || result["name"].Equals(COLUMN_CALL_TYPE))
						{
							migrate = false;
							break;
						}
					}
				}

				if (!migrate)
					return;

				string migrateQuery = "ALTER TABLE {0} ADD COLUMN {1} INTEGER DEFAULT {2}";
				string protocolQuery = string.Format(migrateQuery, TABLE, COLUMN_DIAL_PROTOCOL, 1);
				string callTypeQuery = string.Format(migrateQuery, TABLE, COLUMN_CALL_TYPE, 0);
				using (IcdSqliteCommand command = new IcdSqliteCommand(protocolQuery, connection))
					command.ExecuteNonQuery();
				using (IcdSqliteCommand command = new IcdSqliteCommand(callTypeQuery, connection))
					command.ExecuteNonQuery();
			}
		}

		#endregion
	}
}
