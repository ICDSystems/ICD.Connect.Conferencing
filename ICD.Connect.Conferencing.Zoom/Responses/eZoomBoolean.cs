using System;

namespace ICD.Connect.Conferencing.Zoom.Responses
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
			if (extends == eZoomBoolean.on)
				return true;
			else if (extends == eZoomBoolean.off)
				return false;
			else
				throw new ArgumentOutOfRangeException();
		}
	}
}