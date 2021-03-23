using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Presentation
{
	public enum eSharingState
	{
		None,
		Sending,
		Receiving
	}

	[JsonConverter(typeof(SharingStateInfoConverter))]
	public sealed class SharingStateInfo
	{
		/// <summary>
		/// Gets the paused state.
		/// </summary>
		public bool Paused { get; set; }

		/// <summary>
		/// Gets the sharing state.
		/// </summary>
		public eSharingState State { get; set; }
	}
}