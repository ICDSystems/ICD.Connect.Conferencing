using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;

namespace ICD.Connect.Conferencing.Cisco.Components.System
{
#pragma warning disable 1591
	public enum eH323GatekeeperStatus
	{
		[UsedImplicitly] Inactive,
		[UsedImplicitly] Required,
		[UsedImplicitly] Discovering,
		[UsedImplicitly] Discovered,
		[UsedImplicitly] Authenticating,
		[UsedImplicitly] Authenticated,
		[UsedImplicitly] Registering,
		[UsedImplicitly] Registered,
		[UsedImplicitly] Rejected
	}
#pragma warning restore 1591

	/// <summary>
	/// Event args for use with gatekeeper status events.
	/// </summary>
	public sealed class GatekeeperStatusArgs : GenericEventArgs<eH323GatekeeperStatus>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="status"></param>
		public GatekeeperStatusArgs(eH323GatekeeperStatus status) : base(status)
		{
		}
	}
}
