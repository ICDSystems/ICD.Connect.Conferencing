namespace ICD.Connect.Conferencing.Proxies.Controls.DirectSharing
{
	public static class DirectSharingControlApi
	{
		public const string EVENT_DIRECT_SHARING_ENABLED = "OnDirectShareEnabledChanged";
		public const string EVENT_DIRECT_SHARING_ACTIVE = "OnDirectShareActiveChanged";
		public const string EVENT_SHARING_CODE = "OnSharingCodeChanged";
		public const string EVENT_SHARING_SOURCE_NAME = "OnSharingSourceNameChagned";

		public const string PROPERTY_DIRECT_SHARING_ENABLED = "DirectShareEnabled";
		public const string PROPERTY_DIRECT_SHARING_ACTIVE = "DirectShareActive";
		public const string PROPERTY_DIRECT_SHARING_CODE = "SharingCode";
		public const string PROPERTY_DIRECT_SHARING_SOURCE_NAME = "SharingSourceName";

		public const string EVENT_HELP_DIRECT_SHARING_ENABLED = "Raised when the direct sharing enabled state changes.";
		public const string EVENT_HELP_DIRECT_SHARING_ACTIVE = "Raised when the direct sharing active state changes.";
		public const string EVENT_HELP_SHARING_CODE = "Raised when the direct sharing code changes.";
		public const string EVENT_HELP_SHARING_SOURCE_NAME = "Raised when the direct sharing source name changes.";

		public const string PROPERTY_HELP_DIRECT_SHARING_ENABLED = "Whether or not direct sharing is configured on the device.";
		public const string PROPERTY_HELP_DIRECT_SHARING_ACTIVE = "Whether or not a user is currently sharing content.";
		public const string PROPERTY_HELP_SHARING_CODE = "Some direct sharing devices have a code needed to share.";
		public const string PROPERTY_HELP_SHARING_SOURCE_NAME = "The name of the connected device currently sharing content.";
	}
}
