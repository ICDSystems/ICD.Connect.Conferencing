using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.EventArguments
{
	public enum eInCall
	{
		None = 0,
		Audio = 1,
		Video = 2
	}

	public sealed class InCallEventArgs : GenericEventArgs<eInCall>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public InCallEventArgs(eInCall data)
			: base(data)
		{
		}
	}
}
