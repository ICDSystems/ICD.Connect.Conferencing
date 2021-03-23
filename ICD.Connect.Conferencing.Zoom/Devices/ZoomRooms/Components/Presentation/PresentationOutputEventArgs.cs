using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation
{
	public sealed class PresentationOutputEventArgs : GenericEventArgs<int?>
	{
		public PresentationOutputEventArgs(int? data) : base(data)
		{
		}
	}
}
