using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.DirectSharing;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class DirectSharingCodeApiEventArgs : AbstractGenericApiEventArgs<string>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DirectSharingCodeApiEventArgs(string data)
			: base(DirectSharingControlApi.EVENT_SHARING_CODE, data)
		{
		}
	}
}
