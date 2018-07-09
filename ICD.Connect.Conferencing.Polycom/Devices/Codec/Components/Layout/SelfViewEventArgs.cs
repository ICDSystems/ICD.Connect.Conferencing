using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Layout
{
	public enum eSelfView
	{
		On,
		Off,
		Auto
	}

	public sealed class SelfViewEventArgs : GenericEventArgs<eSelfView>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public SelfViewEventArgs(eSelfView data)
			: base(data)
		{
		}
	}
}
