using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Components.Video
{
	// Ignore no comments warning
#pragma warning disable 1591
	public enum eSelfViewMonitorRole
	{
		[UsedImplicitly] First = 1,
		[UsedImplicitly] Second = 2,
		[UsedImplicitly] Third = 3,
		[UsedImplicitly] Fourth = 4
	}
#pragma warning restore 1591

	/// <summary>
	/// eMonitorRole EventArgs
	/// </summary>
	public sealed class SelfViewMonitorRoleEventArgs : GenericEventArgs<eSelfViewMonitorRole>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="monitor"></param>
		public SelfViewMonitorRoleEventArgs(eSelfViewMonitorRole monitor)
			: base(monitor)
		{
		}
	}
}
