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
		/// Raised when a presentation active state changes.
		/// </summary>
		[ApiEvent(PresentationControlApi.EVENT_PRESENTATION_ACTIVE, PresentationControlApi.HELP_EVENT_PRESENTATION_ACTIVE)]
		event EventHandler<PresentationActiveApiEventArgs> OnPresentationActiveChanged;
		
		/// <summary>
		/// Gets the active presentation input. If this is null, the near side is not presenting.
		/// </summary>
		[ApiProperty(PresentationControlApi.PROPERTY_PRESENTATION_ACTIVE_INPUT, PresentationControlApi.HELP_PROPERTY_PRESENTATION_ACTIVE_INPUT)]
		int? PresentationActiveInput { get; }

		/// <summary>
		/// Gets the active presentation state.
		/// </summary>
		[ApiProperty(PresentationControlApi.PROPERTY_PRESENTATION_ACTIVE, PresentationControlApi.HELP_PROPERTY_PRESENTATION_ACTIVE)]
		bool PresentationActive { get; }

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

	public static class PresentationControlExtensions
	{
		/// <summary>
		/// Gets whether there is an active presentation and the near side is presenting.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool IsNearSidePresenting(this IPresentationControl extends)
		{
			return extends.PresentationActive && extends.PresentationActiveInput != null;
		}

		/// <summary>
		/// Gets whether there is an active presentation and the near side is not presenting.
		/// </summary>
		/// <param name="extends"></param>
		/// <returns></returns>
		public static bool IsFarSidePresenting(this IPresentationControl extends)
		{
			return extends.PresentationActive && extends.PresentationActiveInput == null;
		}
	}
}
