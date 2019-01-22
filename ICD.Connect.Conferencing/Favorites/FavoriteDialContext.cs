using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.Favorites
{
	public sealed class FavoriteDialContext : IDialContext
	{
		/// <summary>
		/// Gets the table id for this instance.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// Instantiates the FavoriteContactMethod from the given contact method.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public static FavoriteDialContext FromDialContext(IDialContext dialContext)
		{
			return new FavoriteDialContext { Protocol = dialContext.Protocol, DialString = dialContext.DialString, CallType = dialContext.CallType };
		}

		#region IDialContext Members

		public eDialProtocol Protocol { get; set; }

		public eCallType CallType { get; set; }

		public string DialString { get; set; }

		#endregion
	}
}
