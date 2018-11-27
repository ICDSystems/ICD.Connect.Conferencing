using ICD.Common.Utils.EventArguments;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Camera
{
	public sealed class ActiveCameraEventArgs : GenericEventArgs<int?>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="data"></param>
		public ActiveCameraEventArgs(int? data)
			: base(data)
		{
		}
	}
}
