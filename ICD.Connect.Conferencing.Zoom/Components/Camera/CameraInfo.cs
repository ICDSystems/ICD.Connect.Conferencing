using ICD.Connect.Conferencing.Zoom.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Components.Camera
{
	[JsonConverter(typeof(CameraInfoConverter))]
	public sealed class CameraInfo
	{
		/// <summary>
		/// ???
		/// Example: "usb:25C1:000D"
		/// </summary>
		public string Alias { get; set; }

		/// <summary>
		/// Name of the camera device.
		/// Example: "ConferenceSHOT 10"
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The id used to change cameras in the Zoom API.
		/// Could possibly change between Windows and Mac Zoom Rooms.
		/// Example: "\\?\usb#vid_25c1&amp;pid_000d&amp;mi_00#6&amp;bf5c205&amp;0&amp;0000#{65e8773d-8f56-11d0-a3b9-00a0c9223196}\global"
		/// </summary>
		public string UsbId { get; set; }
	}
}