using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Content
{
	public sealed class PresentationActiveEventArgs : GenericEventArgs<bool>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public PresentationActiveEventArgs(bool data)
			: base(data)
		{
		}
	}
}
