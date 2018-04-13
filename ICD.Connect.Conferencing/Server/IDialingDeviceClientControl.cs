using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.Controls;

namespace ICD.Connect.Conferencing.Server
{
	public interface IDialingDeviceClientControl : IDialingDeviceControl
	{
		void RaiseSourceAdded(IConferenceSource source);
	}
}