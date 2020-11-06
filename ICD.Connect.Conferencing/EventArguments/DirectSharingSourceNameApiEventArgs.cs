using ICD.Connect.API.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.DirectSharing;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class DirectSharingSourceNameApiEventArgs : AbstractGenericApiEventArgs<string>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public DirectSharingSourceNameApiEventArgs(string data)
			: base(DirectSharingControlApi.EVENT_SHARING_SOURCE_NAME, data)
		{
		}
	}
}
