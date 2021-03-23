using System;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Layout
{
	public struct ZoomLayoutAvailability : IEquatable<ZoomLayoutAvailability>
	{
		/// <summary>
		/// Set to true if it is possible to change the position and size of the thumbnail shown in Speaker view.
		/// </summary>
		public bool CanAdjustFloatingVideo { get; set; }

		/// <summary>
		/// Set to true if it is possible to invoke zConfiguration Call Layout ShareThumb: on,
		/// to put the sharing content into a small video thumbnail.
		/// </summary>
		public bool CanSwitchFloatingShareContent { get; set; }

		/// <summary>
		/// Set to true if it is possible to invoke zConfiguration Call Layout Style: ShareAll,
		/// to switch to the ShareAll mode, where the content sharing is shown full screen on all monitors.
		/// </summary>
		public bool CanSwitchShareOnAllScreens { get; set; }

		/// <summary>
		/// Set to true if it is possible to switch to Speaker view by invoking zConfiguration Call Layout Style: Speaker.
		/// The active speaker is shown full screen, and other video streams, like self-view, are shown in thumbnails.
		/// </summary>
		public bool CanSwitchSpeakerView { get; set; }

		/// <summary>
		/// True if it is possible to invoke zConfiguration Call Layout Style: Gallery, to switch to the Gallery mode,
		/// showing video participants in tiled windows: The Zoom Room shows up to a 5x5 array of tiled windows per page.
		/// </summary>
		public bool CanSwitchWallView { get; set; }

		/// <summary>
		/// Set to true if the ZR is showing the first page of possibly multiple pages of videos.
		/// The UI can show left/right arrow button to flip between pages: When On, gray out the left arrow button.
		/// </summary>
		public bool IsInFirstPage { get; set; }

		/// <summary>
		/// Set to true if the ZR is showing the last page of possibly multiple pages of videos.
		/// On the UI, gray out the right arrow button.
		/// </summary>
		public bool IsInLastPage { get; set; }

		/// <summary>
		/// Set to true if it is possible to change the parameters.
		/// </summary>
		public bool IsSupported { get; set; }

		/// <summary>
		/// For the filmstrip only, specifies the number of thumbnails shown.
		/// </summary>
		public int VideoCountInCurrentPage { get; set; }

		/// <summary>
		/// Indicates which mode applies: Strip or Gallery.
		/// </summary>
		public eZoomLayoutVideoType VideoType { get; set; }

		public override bool Equals(object obj)
		{
			return obj is ZoomLayoutAvailability && Equals((ZoomLayoutAvailability)obj);
		}

		public bool Equals(ZoomLayoutAvailability other)
		{
			return other.CanAdjustFloatingVideo == CanAdjustFloatingVideo &&
			       other.CanSwitchFloatingShareContent == CanSwitchFloatingShareContent &&
			       other.CanSwitchShareOnAllScreens == CanSwitchShareOnAllScreens &&
			       other.CanSwitchSpeakerView == CanSwitchSpeakerView &&
			       other.CanSwitchWallView == CanSwitchWallView &&
			       other.IsInFirstPage == IsInFirstPage &&
			       other.IsInLastPage == IsInLastPage &&
			       other.IsSupported == IsSupported &&
			       other.VideoCountInCurrentPage == VideoCountInCurrentPage &&
			       other.VideoType == VideoType;
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 17;
				hash = hash * 23 + CanAdjustFloatingVideo.GetHashCode();
				hash = hash * 23 + CanSwitchFloatingShareContent.GetHashCode();
				hash = hash * 23 + CanSwitchShareOnAllScreens.GetHashCode();
				hash = hash * 23 + CanSwitchSpeakerView.GetHashCode();
				hash = hash * 23 + CanSwitchWallView.GetHashCode();
				hash = hash * 23 + IsInFirstPage.GetHashCode();
				hash = hash * 23 + IsInLastPage.GetHashCode();
				hash = hash * 23 + IsSupported.GetHashCode();
				hash = hash * 23 + VideoCountInCurrentPage.GetHashCode();
				hash = hash * 23 + VideoType.GetHashCode();
				return hash;
			}
		}
	}
}
