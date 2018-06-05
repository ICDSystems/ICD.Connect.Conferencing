using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.Layout;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class ConferenceLayoutAvailableApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ConferenceLayoutAvailableApiEventArgs(bool data)
			: base(ConferenceLayoutControlApi.EVENT_LAYOUT_AVAILABLE, data)
		{
		}
	}
}