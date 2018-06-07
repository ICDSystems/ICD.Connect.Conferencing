using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.Layout;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceLayoutSelfViewFullScreenApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ConferenceLayoutSelfViewFullScreenApiEventArgs(bool data)
			: base(ConferenceLayoutControlApi.EVENT_SELF_VIEW_FULL_SCREEN_ENABLED, data)
		{
		}
	}
}