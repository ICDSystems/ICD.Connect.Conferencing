using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video
{
	public enum ePipPosition
	{
		[UsedImplicitly] LowerLeft,
		[UsedImplicitly] CenterLeft,
		[UsedImplicitly] UpperLeft,
		[UsedImplicitly] UpperCenter,
		[UsedImplicitly] UpperRight,
		[UsedImplicitly] CenterRight,
		[UsedImplicitly] LowerRight
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
