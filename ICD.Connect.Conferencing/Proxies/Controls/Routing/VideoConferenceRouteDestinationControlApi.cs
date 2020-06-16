namespace ICD.Connect.Conferencing.Proxies.Controls.Routing
{
	public static class VideoConferenceRouteDestinationControlApi
	{
		public const string EVENT_CAMERA_INPUT = "OnCameraInputChanged";
		public const string EVENT_CONTENT_INPUT = "OnContentInputChanged";

		public const string PROPERTY_CAMERA_INPUT = "CameraInput";
		public const string PROPERTY_CONTENT_INPUT = "ContentInput";

		public const string METHOD_GET_CODEC_INPUT_TYPE = "GetCodecInputType";
		public const string METHOD_GET_CODEC_INPUTS = "GetCodecInputs";
		public const string METHOD_SET_CAMERA_INPUT = "SetCameraInput";
		public const string METHOD_SET_CONTENT_INPUT = "SetContentInput";

		public const string HELP_EVENT_CAMERA_INPUT = "Raised when the camera input changes.";
		public const string HELP_EVENT_CONTENT_INPUT = "Raised when the content input changes";

		public const string HELP_PROPERTY_CAMERA_INPUT = "Gets the input address for the camera feed.";
		public const string HELP_PROPERTY_CONTENT_INPUT = "Gets the input address for the content feed.";

		public const string HELP_METHOD_GET_CODEC_INPUT_TYPE = "Gets the codec input type for the input with the given address.";
		public const string HELP_METHOD_GET_CODEC_INPUTS = "Gets the input addresses with the given codec input type.";
		public const string HELP_METHOD_SET_CAMERA_INPUT = "Sets the input address to use for the camera feed.";
		public const string HELP_METHOD_SET_CONTENT_INPUT = "Sets the input address to use for the content feed.";
	}
}
