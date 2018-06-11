using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Polycom.Devices.Components.Content
{
	public sealed class ContentVideoSourceEventArgs : GenericEventArgs<int?>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ContentVideoSourceEventArgs(int? data)
			: base(data)
		{
		}
	}
}