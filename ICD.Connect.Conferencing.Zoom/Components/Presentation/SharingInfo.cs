using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Presentation
{
	public sealed class SharingInfo
	{
		/// <summary>
		/// If the ZR uses a WiFi access point, then the name of that WiFi hot spot appears here.
		/// It will be blank if the ZR has a wired connection.
		/// You need to display that WiFi access point name to the user, so the user can connect the laptop to that WiFi network.
		/// If the ZR is on a wired connection, this entry will be empty.
		/// </summary>
		[JsonProperty("wifiName")]
		public string WifiName { get; private set; }

		/// <summary>
		/// Name of Zoom Room.
		/// </summary>
		[JsonProperty("serverName")]
		public string ServerName { get; private set; }

		/// <summary>
		/// The airplay password, for sharing your iOS device.
		/// It is not the sharing key or the meeting password.
		/// </summary>
		[JsonProperty("password")]
		public string Password { get; private set; }

		/// <summary>
		/// Used in the notification.
		/// When this entry is set to true, you know that you are now in a sharing meeting, and you can remove the airplay.
		/// </summary>
		[JsonProperty("isAirHostClientConnected")]
		public bool IsAirHostClientConnected { get; private set; }

		/// <summary>
		/// Set to on if a compatible HDMI capture device is connected via USB to the Zoom Room.
		/// We support Magewell and INOGENI. (Zoom's supported devices, not Krang's)
		/// </summary>
		[JsonProperty("isBlackMagicConnected")]
		public bool IsBlackMagicConnected { get; private set; }

		/// <summary>
		/// Set to on, if the user has connected an HDMI cable from a laptop to the HDMI capture card,
		/// and the HDMI capture card sees HDMI video coming from the Laptop.
		/// </summary>
		[JsonProperty("isBlackMagicDataAvailable")]
		public bool IsBlackMagicDataAvailable { get; private set; }

		/// <summary>
		/// Whether HDMI is currently actively sharing.
		/// </summary>
		[JsonProperty("isSharingBlackMagic")]
		public bool IsSharingBlackMagic { get; private set; }

		/// <summary>
		/// This is the paring code that is broadcast via an ultrasonic signal from the ZRC.
		/// It is different than the user-supplied paring code.
		/// The ZRC uses a Zoom-proprietary method of advertizing the ultrasonic pairing code,
		/// so it's not possible to advertize it using commonly available libraries.
		/// </summary>
		[JsonProperty("directPresentationPairingCode")]
		public string DirectPresentationPairingCode { get; private set; }

		/// <summary>
		/// The alpha-only sharing key that users type into a laptop client to share with the Zoom Room.
		/// </summary>
		[JsonProperty("directPresentationSharingKey")]
		public string DirectPresentationSharingKey { get; private set; }

		/// <summary>
		/// The laptop has connected to the ZR, either via HDMI, or via network sharing.
		/// </summary>
		[JsonProperty("isDirectPresentationConnected")]
		public bool IsDirectPresentationConnected { get; private set; }

		/// <summary>
		/// Gets the instructions the ZR is displaying on the monitor
		/// </summary>
		[JsonProperty("dispState")]
		public eSharingDisplayState DisplayState { get; private set; }
	}

	public enum eSharingDisplayState
	{
		None,
		Laptop,
		IOS
	}
}