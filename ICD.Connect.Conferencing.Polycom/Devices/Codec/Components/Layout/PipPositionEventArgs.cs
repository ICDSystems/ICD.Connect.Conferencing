using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Layout
{
	public enum ePipPosition
	{
		LowerLeft,
		LowerRight,
		UpperLeft,
		UpperRight,
		Top,
		Right,
		Bottom,
		SideBySide,
		FullScreen
	}

	public sealed class PipPositionEventArgs : GenericEventArgs<ePipPosition>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public PipPositionEventArgs(ePipPosition data)
			: base(data)
		{
		}
	}
}
