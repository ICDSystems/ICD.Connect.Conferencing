namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public enum eBookingSupport
	{
		/// <summary>
		/// Cannot determine if the dialing device supports the booking.
		/// </summary>
		Unknown,

		/// <summary>
		/// Dialing device supports the booking, but may require user interaction.
		/// </summary>
		UserInteractionRequired,

		/// <summary>
		/// Dialing device does not support the booking in any way.
		/// </summary>
		Unsupported,

		/// <summary>
		/// Dialing device could parse a supported booking out of a given booking.
		/// </summary>
		ParsedSupported,

		/// <summary>
		/// Dialing device supports the booking.
		/// </summary>
		Supported,

		/// <summary>
		/// Dialing device could parse a native booking out of a given booking.
		/// </summary>
		ParsedNative,

		/// <summary>
		/// Dialing device natively supports the booking.
		/// Used for special protocols, e.g. Zoom, which have a better experience on native devices.
		/// </summary>
		Native,
	}
}