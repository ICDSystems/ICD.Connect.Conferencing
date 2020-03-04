using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Zoom.Components.Presentation
{
	public sealed class PresentationOutputEventArgs : GenericEventArgs<int?>
	{
		public PresentationOutputEventArgs(int? data) : base(data)
		{
		}
	}
}
