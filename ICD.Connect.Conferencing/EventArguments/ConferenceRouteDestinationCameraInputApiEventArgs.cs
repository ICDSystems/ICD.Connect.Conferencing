using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.Routing;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceRouteDestinationCameraInputApiEventArgs : AbstractGenericApiEventArgs<int?>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ConferenceRouteDestinationCameraInputApiEventArgs(int? data)
			: base(VideoConferenceRouteDestinationControlApi.EVENT_CAMERA_INPUT, data)
		{
		}
	}
}