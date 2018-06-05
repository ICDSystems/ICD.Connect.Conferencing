using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video
{
	public enum ePipPosition
	{
		[UsedImplicitly] CenterLeft,
		[UsedImplicitly] CenterRight,
		[UsedImplicitly] LowerLeft,
		[UsedImplicitly] LowerRight,
		[UsedImplicitly] UpperCenter,
		[UsedImplicitly] UpperLeft,
		[UsedImplicitly] UpperRight
	}

	/// <summary>
	/// ePipPosition EventArgs
	/// </summary>
	public sealed class PipPositionEventArgs : GenericEventArgs<ePipPosition>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="position"></param>
		public PipPositionEventArgs(ePipPosition position) : base(position)
		{
		}
	}
}
