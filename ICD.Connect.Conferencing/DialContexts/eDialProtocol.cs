using System;
using ICD.Connect.Conferencing.EventArguments;

namespace ICD.Connect.Conferencing.DialContexts
{
	/// <summary>
	/// The types of protocols for booking numbers.
	/// Arranged in ascending order of least qualified to most qualified.
	/// </summary>
	public enum eDialProtocol
	{
		Unknown = 0,
		Pstn = 1,
		Sip = 2,
		Zoom = 3,
		ZoomContact = 4,
		ZoomPersonal = 5,
		Spark = 6
	}

	public static class DialProtocolExtensions
	{
		/// <summary>
		/// Gets the most appropriate call type for the given dial protocol
		/// </summary>
		/// <remakrs>
		/// This is not guaranteed to be correct, as many protocols can be used for both audio and video.
		/// Use with caution!
		/// </remakrs>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static eCallType GetCallType(this eDialProtocol extends)
		{
			switch (extends)
			{
				case eDialProtocol.Unknown:
					return eCallType.Unknown;
				case eDialProtocol.Pstn:
					return eCallType.Audio;
				case eDialProtocol.Sip:
				case eDialProtocol.Zoom:
				case eDialProtocol.ZoomContact:
				case eDialProtocol.ZoomPersonal:
				case eDialProtocol.Spark:
					return eCallType.Video;
					
				default:
					throw new ArgumentOutOfRangeException("extends");
			}
		}
	}
}
