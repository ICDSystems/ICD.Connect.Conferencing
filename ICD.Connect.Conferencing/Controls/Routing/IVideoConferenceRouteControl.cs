using System;
using System.Collections.Generic;
using ICD.Connect.API.Attributes;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Proxies.Controls.Routing;
using ICD.Connect.Routing.Controls;

namespace ICD.Connect.Conferencing.Controls.Routing
{
	[ApiClass(typeof(ProxyVideoConferenceRouteControl), typeof(IRouteDestinationControl))]
	public interface IVideoConferenceRouteControl : IRouteDestinationControl, IRouteSourceControl
	{
		/// <summary>
		/// Raised when the camera input changes.
		/// </summary>
		[ApiEvent(VideoConferenceRouteDestinationControlApi.EVENT_CAMERA_INPUT, VideoConferenceRouteDestinationControlApi.HELP_EVENT_CAMERA_INPUT)]
		event EventHandler<ConferenceRouteDestinationCameraInputApiEventArgs> OnCameraInputChanged;

		/// <summary>
		/// Gets the input address for the camera feed.
		/// </summary>
		[ApiProperty(VideoConferenceRouteDestinationControlApi.PROPERTY_CAMERA_INPUT, VideoConferenceRouteDestinationControlApi.HELP_PROPERTY_CAMERA_INPUT)]
		int? CameraInput { get; }

		/// <summary>
		/// Gets the codec input type for the input with the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		[ApiMethod(VideoConferenceRouteDestinationControlApi.METHOD_GET_CODEC_INPUT_TYPE, VideoConferenceRouteDestinationControlApi.HELP_METHOD_GET_CODEC_INPUT_TYPE)]
		eCodecInputType GetCodecInputType(int address);

		/// <summary>
		/// Gets the input addresses with the given codec input type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		[ApiMethod(VideoConferenceRouteDestinationControlApi.METHOD_GET_CODEC_INPUTS, VideoConferenceRouteDestinationControlApi.HELP_METHOD_GET_CODEC_INPUTS)]
		IEnumerable<int> GetCodecInputs(eCodecInputType type);

		/// <summary>
		/// Sets the input address to use for the camera feed.
		/// </summary>
		/// <param name="address"></param>
		[ApiMethod(VideoConferenceRouteDestinationControlApi.METHOD_SET_CAMERA_INPUT, VideoConferenceRouteDestinationControlApi.HELP_METHOD_SET_CAMERA_INPUT)]
		void SetCameraInput(int address);
	}
}
