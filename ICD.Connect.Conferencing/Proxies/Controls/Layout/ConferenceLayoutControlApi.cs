namespace ICD.Connect.Conferencing.Proxies.Controls.Layout
{
	public static class ConferenceLayoutControlApi
	{
		public const string EVENT_LAYOUT_AVAILABLE = "OnLayoutAvailableChanged";
		public const string EVENT_SELF_VIEW_ENABLED = "OnSelfViewEnabledChanged";
		public const string EVENT_SELF_VIEW_FULL_SCREEN_ENABLED = "OnSelfViewFullScreenEnabledChanged";

		public const string PROPERTY_LAYOUT_AVAILABLE = "LayoutAvailable";
		public const string PROPERTY_SELF_VIEW_ENABLED = "SelfViewEnabled";
		public const string PROPERTY_SELF_VIEW_FULL_SCREEN_ENABLED = "SelfViewFullScreenEnabled";

		public const string METHOD_SET_SELF_VIEW_ENABLED = "SetSelfViewEnabled";
		public const string METHOD_SET_SELF_VIEW_FULL_SCREEN_ENABLED = "SetSelfViewFullScreenEnabled";
		public const string METHOD_SET_LAYOUT_MODE = "SetLayoutMode";

		public const string HELP_EVENT_LAYOUT_AVAILABLE = "Raised when layout control becomes available/unavailable.";
		public const string HELP_EVENT_SELF_VIEW_ENABLED = "Raised when the self view enabled state changes.";
		public const string HELP_EVENT_SELF_VIEW_FULL_SCREEN_ENABLED = "Raised when the self view full screen enabled state changes.";

		public const string HELP_PROPERTY_LAYOUT_AVAILABLE = "Returns true if layout control is currently available.";
		public const string HELP_PROPERTY_SELF_VIEW_ENABLED = "Gets the self view enabled state.";
		public const string HELP_PROPERTY_SELF_VIEW_FULL_SCREEN_ENABLED = "Gets the self view fullscreen enabled state.";

		public const string HELP_METHOD_SET_SELF_VIEW_ENABLED = "Enables/disables the self-view window during video conference.";
		public const string HELP_METHOD_SET_SELF_VIEW_FULL_SCREEN_ENABLED = "Enables/disables the self-view fullscreen mode during video conference.";
		public const string HELP_METHOD_SET_LAYOUT_MODE = "Sets the arrangement of UI windows for the video conference.";
	}
}
