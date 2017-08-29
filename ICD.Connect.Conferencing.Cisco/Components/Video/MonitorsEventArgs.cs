using ICD.Common.Utils.EventArguments;
using ICD.Common.Properties;

namespace ICD.Connect.Conferencing.Cisco.Components.Video
{
	public enum eMonitors
	{
		/// <summary>
		/// The same layout is used on all monitors.
		/// </summary>
		[UsedImplicitly] Single,

		/// <summary>
		/// The layout is distributed on two monitors.
		/// </summary>
		[UsedImplicitly] Dual,

		/// <summary>
		/// All participants will be down on the first monitor, while the presentation (if any)
		/// will be shown on the second monitor.
		/// </summary>
		[UsedImplicitly] DualPresentationOnly,

		/// <summary>
		/// The layout is distributed on three monitors.
		/// </summary>
		[UsedImplicitly] Triple,

		/// <summary>
		/// All participants will be down on the first two monitors, while the presentation
		/// (if any) will be shown on the third monitor.
		/// </summary>
		[UsedImplicitly] TriplePresentationOnly,

		/// <summary>
		/// The layout is distributed on four monitors.
		/// </summary>
		[UsedImplicitly] Quadruple
	}

	public sealed class MonitorsEventArgs : GenericEventArgs<eMonitors>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public MonitorsEventArgs(eMonitors data)
			: base(data)
		{
		}
	}
}