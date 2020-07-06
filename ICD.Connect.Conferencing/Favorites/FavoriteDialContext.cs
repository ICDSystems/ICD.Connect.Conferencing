using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Settings.ORM;

namespace ICD.Connect.Conferencing.Favorites
{
	public sealed class FavoriteDialContext : AbstractDialContext
	{
		/// <summary>
		/// Gets the table id for this instance.
		/// </summary>
		[PrimaryKey]
		[System.Reflection.Obfuscation(Exclude = true)]
		public int Id { get; set; }

		/// <summary>
		/// Gets the id of the parent favorite contact.
		/// </summary>
		[ForeignKey(typeof(Favorite))]
		[System.Reflection.Obfuscation(Exclude = true)]
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

		/// <summary>
		/// Gets the protocol for placing the call.
		/// </summary>
		[DataField]
		[System.Reflection.Obfuscation(Exclude = true)]
		public override eDialProtocol Protocol { get; set; }

		/// <summary>
		/// Gets the type of call.
		/// </summary>
		[DataField]
		[System.Reflection.Obfuscation(Exclude = true)]
		public override eCallType CallType { get; set; }

		/// <summary>
		/// Gets the number, uri, etc for placing the call.
		/// </summary>
		[DataField]
		[System.Reflection.Obfuscation(Exclude = true)]
		public override string DialString { get; set; }

		/// <summary>
		/// Gets the password for joining the call.
		/// </summary>
		[DataField]
		[System.Reflection.Obfuscation(Exclude = true)]
		public override string Password { get; set; }

		#endregion
	}
}
