using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.DirectSharing;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class DirectSharingEnabledApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DirectSharingEnabledApiEventArgs(bool data)
			: base(DirectSharingControlApi.EVENT_DIRECT_SHARING_ENABLED, data)
		{
		}
	}
}
