using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.Routing;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceRouteDestinationContentInputApiEventArgs : AbstractGenericApiEventArgs<int?>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ConferenceRouteDestinationContentInputApiEventArgs(int? data)
			: base(VideoConferenceRouteDestinationControlApi.EVENT_CONTENT_INPUT, data)
		{
		}
	}
}