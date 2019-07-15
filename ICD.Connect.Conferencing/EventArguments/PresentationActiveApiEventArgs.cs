using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.Presentation;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class PresentationActiveApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public PresentationActiveApiEventArgs(bool data)
			: base(PresentationControlApi.EVENT_PRESENTATION_ACTIVE, data)
		{
		}
	}
}
