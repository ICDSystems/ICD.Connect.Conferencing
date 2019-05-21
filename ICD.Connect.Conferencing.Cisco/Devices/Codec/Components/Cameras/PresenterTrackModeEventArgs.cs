using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras
{
	public enum ePresenterTrackMode
	{
		Off,
		Follow,
		Diagnostic,
		Background,
		Setup,
		Persistent
	}

	public sealed class PresenterTrackModeEventArgs : GenericEventArgs<ePresenterTrackMode>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public PresenterTrackModeEventArgs(ePresenterTrackMode data)
			: base(data)
		{
		}
	}
}
