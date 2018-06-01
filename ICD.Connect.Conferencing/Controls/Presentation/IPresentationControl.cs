using System;
using ICD.Connect.API.Attributes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.Presentation;
using ICD.Connect.Devices.Controls;

namespace ICD.Connect.Conferencing.Controls.Presentation
{
	[ApiClass(typeof(ProxyPresentationControl), typeof(IDeviceControl))]
	public interface IPresentationControl : IDeviceControl
	{
		/// <summary>
		/// Raised when the presentation active input changes.
		/// </summary>
		[ApiEvent(PresentationControlApi.EVENT_PRESENTATION_ACTIVE_INPUT, PresentationControlApi.HELP_EVENT_PRESENTATION_ACTIVE_INPUT)]
		event EventHandler<PresentationActiveInputApiEventArgs> OnPresentationActiveInputChanged;

		/// <summary>
		/// Gets the active presentation input.
		/// </summary>
		[ApiProperty(PresentationControlApi.PROPERTY_PRESENTATION_ACTIVE_INPUT, PresentationControlApi.HELP_PROPERTY_PRESENTATION_ACTIVE_INPUT)]
		int? PresentationActiveInput { get; }

		/// <summary>
		/// Starts presenting the source at the given input address.
		/// </summary>
		/// <param name="input"></param>
		[ApiMethod(PresentationControlApi.METHOD_START_PRESENTATION, PresentationControlApi.HELP_METHOD_START_PRESENTATION)]
		void StartPresentation(int input);

		/// <summary>
		/// Stops the active presentation.
		/// </summary>
		[ApiMethod(PresentationControlApi.METHOD_STOP_PRESENTATION, PresentationControlApi.HELP_METHOD_STOP_PRESENTATION)]
		void StopPresentation();
	}
}
