using System;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses
{
	public enum eZoomBoolean
	{
		off,
		on
	}

	public static class ZoomBooleanExtensions
	{
		public static bool ToBool(this eZoomBoolean extends)
		{
			switch (extends)
			{
				case eZoomBoolean.on:
					return true;
				case eZoomBoolean.off:
					return false;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}