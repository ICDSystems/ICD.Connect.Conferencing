#if NETFRAMEWORK
extern alias RealNewtonsoft;
using RealNewtonsoft.Newtonsoft.Json;
#else
using Newtonsoft.Json;
#endif
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses.Converters;

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