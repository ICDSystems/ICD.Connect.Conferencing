using ICD.Connect.Conferencing.Zoom.Responses.Attributes;
using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("Client", eZoomRoomApiType.zConfiguration, false),
	 ZoomRoomApiResponse("Client", eZoomRoomApiType.zConfiguration, true)]
	[JsonConverter(typeof(ClientCallLayoutResponseConverter))]
	public sealed class ClientCallLayoutResponse : AbstractZoomRoomResponse
	{
		public CallLayoutConfiguration CallLayoutConfiguration { get; set; }
	}

	[JsonConverter(typeof(CallLayoutResponseConverter))]
	public sealed class CallLayoutConfiguration
	{
		public LayoutConfigurationHeader LayoutConfigurationHeader { get; set; }
	}

	[JsonConverter(typeof(LayoutConfigurationHeaderConverter))]
	public sealed class LayoutConfigurationHeader
	{
		public LayoutConfiguration LayoutConfiguration { get; set; }
	}

	[JsonConverter(typeof(LayoutConfigurationConverter))]
	public sealed class LayoutConfiguration
	{
		/// <summary>
		/// On for share content in thumbnail off for camera content
		/// </summary>
		public bool ShareThumb { get; set; }

		public eZoomLayoutStyle Style { get; set; }

		public eZoomLayoutSize Size { get; set; }

		public eZoomLayoutPosition Position { get; set; }
	}

	public enum eZoomLayoutStyle
	{
		None = 0,
		Gallery = 1,
		Speaker = 2,
		Strip = 3,
		ShareAll = 4
	}

	public enum eZoomLayoutSize
	{
		None = 0,
		Size1 = 1,
		Size2 = 2,
		Size3 = 3,
		Strip = 4,
		Off = 5
	}

	public enum eZoomLayoutPosition
	{
		None = 0,
		UpRight = 1,
		DownRight = 2,
		UpLeft = 3,
		DownLeft = 4,
		Center = 5,
		Up = 6,
		Right = 7,
		Down = 8,
		Left = 9
	}

	[ZoomRoomApiResponse("Layout", eZoomRoomApiType.zStatus, false),
	 ZoomRoomApiResponse("Layout", eZoomRoomApiType.zStatus, true)]
	[JsonConverter(typeof(CallLayoutStatusResponseConverter))]
	public sealed class CallLayoutStatusResponse : AbstractZoomRoomResponse
	{
		public CallLayoutStatus LayoutStatus { get; set; }
	}

	[JsonConverter(typeof(CallLayoutStatusConverter))]
	public sealed class CallLayoutStatus
	{
		public bool CanAdjustFloatingVideo { get; set; }

		public bool CanSwitchFloatingShareContent { get; set; }

		public bool CanSwitchShareOnAllScreens { get; set; }

		public bool CanSwitchSpeakerView { get; set; }

		public bool CanSwitchWallView { get; set; }

		public bool IsInFirstPage { get; set; }

		public bool IsInLastPage { get; set; }

		public bool IsSupported { get; set; }

		public int VideoCountInCurrentPage { get; set; }

		public eZoomLayoutVideoType VideoType { get; set; }
	}

	public enum eZoomLayoutVideoType
	{
		None = 0,
		Strip = 1,
		Gallery = 2
	}
}
