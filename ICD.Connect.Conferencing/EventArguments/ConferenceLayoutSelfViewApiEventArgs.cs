using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.Layout;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceLayoutSelfViewApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ConferenceLayoutSelfViewApiEventArgs(bool data)
			: base(ConferenceLayoutControlApi.EVENT_SELF_VIEW_ENABLED, data)
		{
		}
	}
}