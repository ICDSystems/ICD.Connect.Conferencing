using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Presentation
{
	[JsonConverter(typeof(PinStatusOfScreenNotificationConverter))]
	public sealed class PinStatusOfScreenNotification
	{
		public int ScreenIndex { get; set; }
		
		public bool CanBePinned { get; set; }
		
		public bool CanPinShare { get; set; }
		
		public string PinnedUserId { get; set; }
		
		public eZoomScreenLayout ScreenLayout { get; set; }
		
		public int PinnedShareSourceId { get; set; }
		
		public int ShareSourceType { get; set; }
		
		public string WhyCannotPinShare { get; set; }
	}

	public enum eZoomScreenLayout
	{
		Speaker = 0,
		SelfView = 1,
		Gallery = 4,
		ShareContent = 5
	}
}