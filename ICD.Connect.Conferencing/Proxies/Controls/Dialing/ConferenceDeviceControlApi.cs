namespace ICD.Connect.Conferencing.Proxies.Controls.Dialing
{
	public static class ConferenceDeviceControlApi
	{
		public const string EVENT_SUPPORTED_CONFERENCE_FEATURES_CHANGED = "OnSupportedConferenceFeaturesChanged";

		public const string HELP_EVENT_SUPPORTED_CONFERENCE_FEATURES_CHANGED = "Raised when the supported conference features change.";

		public const string PROPERTY_SUPPORTED_CONFERENCE_FEATURES = "SupportedConferenceFeatures";
		public const string PROPERTY_AUTO_ANSWER = "AutoAnswer";
		public const string PROPERTY_PRIVACY_MUTED = "PrivacyMuted";
		public const string PROPERTY_DO_NOT_DISTURB = "DoNotDisturb";
		public const string PROPERTY_CAMERA_ENABLED = "CameraEnabled";
		public const string PROPERTY_SUPPORTS = "Supports";

		public const string HELP_PROPERTY_SUPPORTED_CONFERENCE_FEATURES =
			"Returns the features that are supported by this conference control.";

		public const string HELP_PROPERTY_AUTO_ANSWER = "The AutoAnswer state.";
		public const string HELP_PROPERTY_PRIVACY_MUTED = "The current microphone mute state.";
		public const string HELP_PROPERTY_DO_NOT_DISTURB = "The DoNotDisturb state.";
		public const string HELP_PROPERTY_CAMERA_ENABLED = "The current camera enabled state.";
		public const string HELP_PROPERTY_SUPPORTS = "Gets the type of conference this dialer supports.";

		public const string METHOD_CAN_DIAL = "CanDialContext";
		public const string METHOD_DIAL_CONTEXT = "DialContext";
		public const string METHOD_SET_DO_NOT_DISTURB = "SetDoNotDisturb";
		public const string METHOD_SET_AUTO_ANSWER = "SetAutoAnswer";
		public const string METHOD_SET_PRIVACY_MUTE = "SetPrivacyMute";
		public const string METHOD_SET_CAMERA_ENABLED = "SetCameraEnabled";

		public const string HELP_METHOD_CAN_DIAL =
			"Returns the level of support the device has for the given context.";

		public const string HELP_METHOD_DIAL_CONTEXT = "Dials the given context.";
		public const string HELP_METHOD_SET_DO_NOT_DISTURB = "Sets the do-not-disturb enabled state.";
		public const string HELP_METHOD_SET_AUTO_ANSWER = "SetAutoAnswer";
		public const string HELP_METHOD_SET_PRIVACY_MUTE = "SetPrivacyMute";
		public const string HELP_METHOD_SET_CAMERA_ENABLED = "SetCameraEnabled";
	}
}
