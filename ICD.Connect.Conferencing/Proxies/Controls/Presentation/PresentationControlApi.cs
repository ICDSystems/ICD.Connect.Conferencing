namespace ICD.Connect.Conferencing.Proxies.Controls.Presentation
{
	public static class PresentationControlApi
	{
		public const string EVENT_PRESENTATION_ACTIVE_INPUT = "OnPresentationActiveInputChanged";

		public const string PROPERTY_PRESENTATION_ACTIVE_INPUT = "PresentationActiveInput";

		public const string METHOD_START_PRESENTATION = "StartPresentation";
		public const string METHOD_STOP_PRESENTATION = "StopPresentation";

		public const string HELP_EVENT_PRESENTATION_ACTIVE_INPUT = "Raised when the presentation active input changes.";

		public const string HELP_PROPERTY_PRESENTATION_ACTIVE_INPUT = "Gets the active presentation input.";

		public const string HELP_METHOD_START_PRESENTATION = "Starts presenting the source at the given input address.";
		public const string HELP_METHOD_STOP_PRESENTATION = "Stops the active presentation.";
	}
}
