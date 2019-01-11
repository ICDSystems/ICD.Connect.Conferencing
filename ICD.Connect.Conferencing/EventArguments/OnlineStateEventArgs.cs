using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.EventArguments
{
	public sealed class OnlineStateEventArgs : GenericEventArgs<eOnlineState>
	{
		public OnlineStateEventArgs(eOnlineState data) : base(data)
		{
		}
	}
}