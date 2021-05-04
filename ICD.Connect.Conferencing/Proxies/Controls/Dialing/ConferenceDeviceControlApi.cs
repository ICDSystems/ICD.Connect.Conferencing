namespace ICD.Connect.Conferencing.Proxies.Controls.Dialing
{
	public static class ConferenceDeviceControlApi
	{
		#region Events

		public const string EVENT_SUPPORTED_CONFERENCE_FEATURES_CHANGED = "OnSupportedConferenceFeaturesChanged";

		#endregion

		#region Event Help

		public const string HELP_EVENT_SUPPORTED_CONFERENCE_FEATURES_CHANGED = "Raised when the supported conference features change.";

		#endregion

		#region Properties

		public const string PROPERTY_SUPPORTED_CONFERENCE_FEATURES = "SupportedConferenceControlFeatures";
		public const string PROPERTY_AUTO_ANSWER = "AutoAnswer";
		public const string PROPERTY_PRIVACY_MUTED = "PrivacyMuted";
		public const string PROPERTY_DO_NOT_DISTURB = "DoNotDisturb";
		public const string PROPERTY_CAMERA_MUTE = "CameraMute";
		public const string PROPERTY_SUPPORTS = "Supports";
		public const string PROPERTY_SIP_IS_REGISTERED = "SipIsRegistered";
		public const string PROPERTY_SIP_LOCAL_NAME = "SipLocalName";
		public const string PROPERTY_SIP_REGISTRATION_STATUS = "SipRegistrationStatus";
		public const string PROPERTY_AM_I_HOST = "AmIHost";
		public const string PROPERTY_CALL_LOCK = "CallLock";

		#endregion

		#region Property Help

		public const string HELP_PROPERTY_SUPPORTED_CONFERENCE_FEATURES =
			"Returns the features that are supported by this conference control.";

		public const string HELP_PROPERTY_AUTO_ANSWER = "The AutoAnswer state.";
		public const string HELP_PROPERTY_PRIVACY_MUTED = "The current microphone mute state.";
		public const string HELP_PROPERTY_DO_NOT_DISTURB = "The DoNotDisturb state.";
		public const string HELP_PROPERTY_CAMERA_MUTE = "The current camera mute state.";
		public const string HELP_PROPERTY_SUPPORTS = "Gets the type of conference this dialer supports.";
		public const string HELP_PROPERTY_SIP_IS_REGISTERED = "Gets the sip registration state.";
		public const string HELP_PROPERTY_SIP_LOCAL_NAME = "Gets the sip local name.";
		public const string HELP_PROPERTY_SIP_REGISTRATION_STATUS = "Gets the sip registration status";
		public const string HELP_PROPERTY_AM_I_HOST = "Whether or not the controled device is the host of the conference.";
		public const string HELP_PROPERTY_CALL_LOCK = "Gets the call lock state.";

		#endregion

		#region Methods

		public const string METHOD_GET_CONFERENCES = "GetConferences";
		public const string METHOD_CAN_DIAL = "CanDialContext";
		public const string METHOD_DIAL_CONTEXT = "DialContext";
		public const string METHOD_SET_DO_NOT_DISTURB = "SetDoNotDisturb";
		public const string METHOD_SET_AUTO_ANSWER = "SetAutoAnswer";
		public const string METHOD_SET_PRIVACY_MUTE = "SetPrivacyMute";
		public const string METHOD_SET_CAMERA_MUTE = "SetCameraMute";
		public const string METHOD_START_PERSONAL_MEETING = "StartPersonalMeeting";
		public const string METHOD_ENABLE_CALL_LOCK = "EnableCallLock";

		#endregion

		#region Method Help

		public const string HELP_METHOD_GET_CONFERENCES = "Gets the active conference sources";
		public const string HELP_METHOD_CAN_DIAL =
			"Returns the level of support the device has for the given context.";

		public const string HELP_METHOD_DIAL_CONTEXT = "Dials the given context.";
		public const string HELP_METHOD_SET_DO_NOT_DISTURB = "Sets the do-not-disturb enabled state.";
		public const string HELP_METHOD_SET_AUTO_ANSWER = "SetAutoAnswer";
		public const string HELP_METHOD_SET_PRIVACY_MUTE = "SetPrivacyMute";
		public const string HELP_METHOD_SET_CAMERA_MUTE = "SetCameraMute";
		public const string HELP_METHOD_START_PERSONAL_MEETING = "Starts a personal meeting.";
		public const string HELP_METHOD_ENABLE_CALL_LOCK = "Locks the current active conference so no more participants may join.";

		#endregion
	}
}
