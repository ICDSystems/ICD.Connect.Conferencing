using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System
{
	/// <summary>
	/// Registration values
	/// </summary>
	public enum eRegState
	{
		Unknown,
		Deregister,
		Failed,
		Inactive,
		Registered,
		Registering
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
