using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Camera
{
	public class CameraInfo
	{
		/// <summary>
		/// ???
		/// Example: "usb:25C1:000D"
		/// </summary>
		[JsonProperty("Alias")]
		public string Alias { get; private set; }

		/// <summary>
		/// Name of the camera device.
		/// Example: "ConferenceSHOT 10"
		/// </summary>
		[JsonProperty("Name")]
		public string Name { get; private set; }

		/// <summary>
		/// The id used to change cameras in the Zoom API.
		/// Could possibly change between Windows and Mac Zoom Rooms.
		/// Example: "\\?\usb#vid_25c1&amp;pid_000d&amp;mi_00#6&amp;bf5c205&amp;0&amp;0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\global"
		/// </summary>
		[JsonProperty("id")]
		public string UsbId { get; private set; }
	}
}