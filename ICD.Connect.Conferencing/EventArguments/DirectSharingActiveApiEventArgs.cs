using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.DirectSharing;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class DirectSharingActiveApiEventArgs : AbstractGenericApiEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DirectSharingActiveApiEventArgs(bool data)
			: base(DirectSharingControlApi.EVENT_DIRECT_SHARING_ACTIVE, data)
		{
		}
	}
}
