using ICD.Connect.API.Attributes;
using ICD.Connect.Conferencing.Proxies.Controls.Layout;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Layout
{
	[ApiClass(typeof(ProxyConferenceLayoutControl), typeof(IDeviceControl))]
	public interface IConferenceLayoutControl : IDeviceControl
	{
		/// <summary>
		/// Enables/disables the self-view window during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		[ApiMethod(ConferenceLayoutControlApi.METHOD_SET_SELF_VIEW_ENABLED, ConferenceLayoutControlApi.HELP_METHOD_SET_SELF_VIEW_ENABLED)]
		void SetSelfViewEnabled(bool enabled);
	}
}
