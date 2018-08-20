namespace ICD.Connect.Conferencing.Proxies.Controls.Dialing
{
	public static class DialingDeviceControlApi
	{
		public const string PROPERTY_AUTO_ANSWER = "AutoAnswer";
		public const string PROPERTY_PRIVACY_MUTED = "PrivacyMuted";
		public const string PROPERTY_DO_NOT_DISTURB = "DoNotDisturb";
		public const string PROPERTY_SUPPORTS = "Supports";

		public const string HELP_PROPERTY_AUTO_ANSWER = "The AutoAnswer state.";
		public const string HELP_PROPERTY_PRIVACY_MUTED = "The current microphone mute state.";
		public const string HELP_PROPERTY_DO_NOT_DISTURB = "The DoNotDisturb state.";
		public const string HELP_PROPERTY_SUPPORTS = "Gets the type of conference this dialer supports.";

		public const string METHOD_DIAL = "Dial";
		public const string METHOD_DIAL_TYPE = "DialType";
		public const string METHOD_DIAL_CONTACT = "DialContact";
		public const string METHOD_SET_DO_NOT_DISTURB = "SetDoNotDisturb";
		public const string METHOD_SET_AUTO_ANSWER = "SetAutoAnswer";
		public const string METHOD_SET_PRIVACY_MUTE = "SetPrivacyMute";

		public const string HELP_METHOD_DIAL = "Dials the given number.";
		public const string HELP_METHOD_DIAL_TYPE = "Dials the given number.";
		public const string HELP_METHOD_DIAL_CONTACT = "Dials the given contact.";
		public const string HELP_METHOD_SET_DO_NOT_DISTURB = "Sets the do-not-disturb enabled state.";
		public const string HELP_METHOD_SET_AUTO_ANSWER = "SetAutoAnswer";
		public const string HELP_METHOD_SET_PRIVACY_MUTE = "SetPrivacyMute";
	}
}
