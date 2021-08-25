#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation
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