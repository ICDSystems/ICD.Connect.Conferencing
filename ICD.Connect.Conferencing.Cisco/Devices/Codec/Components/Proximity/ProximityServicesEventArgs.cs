using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Proximity
{
	public enum eProximityServiceState
	{
		[PublicAPI] Enabled,
		[PublicAPI] Disabled
	}

	/// <summary>
	/// Args used when one of the codec proximity services enabled state changes
	/// </summary>
	public sealed class ProximityServicesEventArgs : GenericEventArgs<eProximityServiceState>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ProximityServicesEventArgs(eProximityServiceState data)
			: base(data)
		{
		}
	}
}
