using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Presentation
{
	// Ignore missing comments
#pragma warning disable 1591
	public enum ePresentationMode
	{
		[PublicAPI] Off,
		[PublicAPI] Sending,
		[PublicAPI] Receiving
	}
#pragma warning restore 1591

	/// <summary>
	/// Args used when the codec presentation mode changes.
	/// </summary>
	public sealed class PresentationModeEventArgs : GenericEventArgs<ePresentationMode>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="presentationMode"></param>
		public PresentationModeEventArgs(ePresentationMode presentationMode)
			: base(presentationMode)
		{
		}
	}
}
