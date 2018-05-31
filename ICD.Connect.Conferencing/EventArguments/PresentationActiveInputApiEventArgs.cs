using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.Presentation;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class PresentationActiveInputApiEventArgs : AbstractGenericApiEventArgs<int?>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public PresentationActiveInputApiEventArgs(int? data)
			: base(PresentationControlApi.EVENT_PRESENTATION_ACTIVE, data)
		{
		}
	}
}
