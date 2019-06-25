using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.Layout;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Layout
{
	public enum eLayoutMode
	{
		Auto,
		Custom,
		Equal,
		Overlay,
		Prominent,
		Single
	}

	[ApiClass(typeof(ProxyConferenceLayoutControl), typeof(IDeviceControl))]
	public interface IConferenceLayoutControl : IDeviceControl
	{
		/// <summary>
		/// Raised when layout control becomes available/unavailable.
		/// </summary>
		[ApiEvent(ConferenceLayoutControlApi.EVENT_LAYOUT_AVAILABLE, ConferenceLayoutControlApi.HELP_EVENT_LAYOUT_AVAILABLE)]
		event EventHandler<ConferenceLayoutAvailableApiEventArgs> OnLayoutAvailableChanged;

		/// <summary>
		/// Raised when the self view enabled state changes.
		/// </summary>
		[ApiEvent(ConferenceLayoutControlApi.EVENT_SELF_VIEW_ENABLED, ConferenceLayoutControlApi.HELP_EVENT_SELF_VIEW_ENABLED)]
		event EventHandler<ConferenceLayoutSelfViewApiEventArgs> OnSelfViewEnabledChanged;

		/// <summary>
		/// Raised when the self view full screen enabled state changes.
		/// </summary>
		[ApiEvent(ConferenceLayoutControlApi.EVENT_SELF_VIEW_FULL_SCREEN_ENABLED, ConferenceLayoutControlApi.HELP_EVENT_SELF_VIEW_FULL_SCREEN_ENABLED)]
		event EventHandler<ConferenceLayoutSelfViewFullScreenApiEventArgs> OnSelfViewFullScreenEnabledChanged;

		#region Properties

		/// <summary>
		/// Returns true if layout control is currently available.
		/// Some conferencing devices only support layout in certain configurations (e.g. single display mode).
		/// </summary>
		[ApiProperty(ConferenceLayoutControlApi.PROPERTY_LAYOUT_AVAILABLE, ConferenceLayoutControlApi.HELP_PROPERTY_LAYOUT_AVAILABLE)]
		bool LayoutAvailable { get; }

		/// <summary>
		/// Gets the self view enabled state.
		/// </summary>
		[ApiProperty(ConferenceLayoutControlApi.PROPERTY_SELF_VIEW_ENABLED, ConferenceLayoutControlApi.HELP_PROPERTY_SELF_VIEW_ENABLED)]
		bool SelfViewEnabled { get; }

		/// <summary>
		/// Gets the self view fullscreen enabled state.
		/// </summary>
		[ApiProperty(ConferenceLayoutControlApi.PROPERTY_SELF_VIEW_FULL_SCREEN_ENABLED, ConferenceLayoutControlApi.HELP_PROPERTY_SELF_VIEW_FULL_SCREEN_ENABLED)]
		bool SelfViewFullScreenEnabled { get; }

		#endregion

		#region Methods

		/// <summary>
		/// Enables/disables the self-view window during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		[ApiMethod(ConferenceLayoutControlApi.METHOD_SET_SELF_VIEW_ENABLED, ConferenceLayoutControlApi.HELP_METHOD_SET_SELF_VIEW_ENABLED)]
		void SetSelfViewEnabled(bool enabled);

		/// <summary>
		/// Enables/disables the self-view fullscreen mode during video conference.
		/// </summary>
		/// <param name="enabled"></param>
		[ApiMethod(ConferenceLayoutControlApi.METHOD_SET_SELF_VIEW_FULL_SCREEN_ENABLED, ConferenceLayoutControlApi.HELP_METHOD_SET_SELF_VIEW_FULL_SCREEN_ENABLED)]
		void SetSelfViewFullScreenEnabled(bool enabled);

		/// <summary>
		/// Sets the arrangement of UI windows for the video conference.
		/// </summary>
		/// <param name="mode"></param>
		[ApiMethod(ConferenceLayoutControlApi.METHOD_SET_LAYOUT_MODE, ConferenceLayoutControlApi.HELP_METHOD_SET_LAYOUT_MODE)]
		void SetLayoutMode(eLayoutMode mode);

		#endregion
	}
}
