using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Settings.ORM;

namespace ICD.Connect.Conferencing.Favorites
{
	public sealed class FavoriteDialContext : IDialContext
	{
		/// <summary>
		/// Gets the table id for this instance.
		/// </summary>
		[PrimaryKey]
		public int Id { get; set; }

		[ForeignKey(typeof(Favorite))]
		public int FavoriteId { get; set; }

		/// <summary>
		/// Instantiates the FavoriteContactMethod from the given contact method.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public static FavoriteDialContext FromDialContext(IDialContext dialContext)
		{
			return new FavoriteDialContext
			{
				Protocol = dialContext.Protocol,
				DialString = dialContext.DialString,
				CallType = dialContext.CallType,
				Password = dialContext.Password
			};
		}

		#region IDialContext Members

		[DataField]
		public eDialProtocol Protocol { get; set; }

		[DataField]
		public eCallType CallType { get; set; }

		[DataField]
		public string DialString { get; set; }

		[DataField]
		public string Password { get; set; }

		#endregion
	}
}
