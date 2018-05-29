using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls
{
	public interface IPresentationControl : IDeviceControl
	{
		/// <summary>
		/// Starts presenting the source at the given input address.
		/// </summary>
		/// <param name="input"></param>
		void StartPresentation(int input);
	}
}
