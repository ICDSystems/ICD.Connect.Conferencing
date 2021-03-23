using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation
{
	public enum eSharingDisplayState
	{
		None,
		Laptop,
		IOS
	}

	[JsonConverter(typeof(SharingInfoConverter))]
	public sealed class SharingInfo
	{
		/// <summary>
		/// If the ZR uses a WiFi access point, then the name of that WiFi hot spot appears here.
		/// It will be blank if the ZR has a wired connection.
		/// You need to display that WiFi access point name to the user, so the user can connect the laptop to that WiFi network.
		/// If the ZR is on a wired connection, this entry will be empty.
		/// </summary>
		public string WifiName { get; set; }

		/// <summary>
		/// Name of Zoom Room.
		/// </summary>
		public string ServerName { get; set; }

		/// <summary>
		/// The airplay password, for sharing your iOS device.
		/// It is not the sharing key or the meeting password.
		/// </summary>
		public string Password { get; set; }

		/// <summary>
		/// Used in the notification.
		/// When this entry is set to true, you know that you are now in a sharing meeting, and you can remove the airplay.
		/// </summary>
		public bool IsAirHostClientConnected { get; set; }

		/// <summary>
		/// Set to on if a compatible HDMI capture device is connected via USB to the Zoom Room.
		/// We support Magewell and INOGENI. (Zoom's supported devices, not Krang's)
		/// </summary>
		public bool IsBlackMagicConnected { get; set; }

		/// <summary>
		/// Set to on, if the user has connected an HDMI cable from a laptop to the HDMI capture card,
		/// and the HDMI capture card sees HDMI video coming from the Laptop.
		/// </summary>
		public bool IsBlackMagicDataAvailable { get; set; }

		/// <summary>
		/// Whether HDMI is currently actively sharing.
		/// </summary>
		public bool IsSharingBlackMagic { get; set; }

		/// <summary>
		/// This is the paring code that is broadcast via an ultrasonic signal from the ZRC.
		/// It is different than the user-supplied paring code.
		/// The ZRC uses a Zoom-proprietary method of advertizing the ultrasonic pairing code,
		/// so it's not possible to advertize it using commonly available libraries.
		/// </summary>
		public string DirectPresentationPairingCode { get; set; }

		/// <summary>
		/// The alpha-only sharing key that users type into a laptop client to share with the Zoom Room.
		/// </summary>
		public string DirectPresentationSharingKey { get; set; }

		/// <summary>
		/// The laptop has connected to the ZR, either via HDMI, or via network sharing.
		/// </summary>
		public bool IsDirectPresentationConnected { get; set; }

		/// <summary>
		/// Gets the instructions the ZR is displaying on the monitor
		/// </summary>
		public eSharingDisplayState DisplayState { get; set; }
	}
}