using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System
{
	/// <summary>
	/// Registration values
	/// </summary>
	public enum eRegState
	{
		// Ignore missing comment warnings
#pragma warning disable 1591
		[PublicAPI] Unknown,
		[PublicAPI] Deregister,
		[PublicAPI] Failed,
		[PublicAPI] Inactive,
		[PublicAPI] Registered,
		[PublicAPI] Registering
#pragma warning restore 1591
	};

	/// <summary>
	/// Registration Event Data carrier
	/// </summary>
	public sealed class RegistrationEventArgs : GenericEventArgs<eRegState>
	{
		/// <summary>
		/// Priamry Constructor
		/// </summary>
		/// <param name="state"></param>
		public RegistrationEventArgs(eRegState state)
			: base(state)
		{
		}
	}
}
