namespace ICD.Connect.Conferencing.Proxies.Controls.Routing
{
	public static class VideoConferenceRouteDestinationControlApi
	{
		public const string METHOD_GET_CODEC_INPUT_TYPE = "GetCodecInputType";
		public const string METHOD_GET_CODEC_INPUTS = "GetCodecInputs";
		public const string METHOD_SET_CAMERA_INPUT = "SetCameraInput";

		public const string HELP_METHOD_GET_CODEC_INPUT_TYPE = "Gets the codec input type for the input with the given address.";
		public const string HELP_METHOD_GET_CODEC_INPUTS = "Gets the input addresses with the given codec input type.";
		public const string HELP_METHOD_SET_CAMERA_INPUT = "Sets the input address to use for the camera feed.";
	}
}
