using System;
using System.Collections.Generic;
using System.Text;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	[ZoomRoomApiResponse("BookingsUpdateResult", eZoomRoomApiType.zCommand, true)]
	public sealed class BookingsUpdateResponse : AbstractZoomRoomResponse
	{
	}
}
