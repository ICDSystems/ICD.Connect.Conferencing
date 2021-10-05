using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Proximity
{
	public enum eProximityMode
	{
		[PublicAPI] Unknown, // Cloud-Registred Codecs don't have proximity mode
		[PublicAPI] Off,
		[PublicAPI] On
	}

	/// <summary>
	/// Args used when the codec proximity mode changes.
	/// </summary>
	public sealed class ProximityModeEventArgs : GenericEventArgs<eProximityMode>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ProximityModeEventArgs(eProximityMode data)
			: base(data)
		{
		}
	}
}
