using System;
using System.Collections.Generic;
using ICD.Connect.Conferencing.Controls.Routing;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.EventArguments;

namespace ICD.Connect.Conferencing.Mock
{
	public sealed class MockVideoConferenceRouteControl : AbstractVideoConferenceRouteControl<MockConferencingDevice>
	{
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public MockVideoConferenceRouteControl(MockConferencingDevice parent, int id) : base(parent, id)
		{
		}

		/// <summary>
		/// Returns true if a signal is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			return input < 4 && input > 0;
		}

		/// <summary>
		/// Returns the true if the input is actively being used by the source device.
		/// For example, a display might true if the input is currently on screen,
		/// while a switcher may return true if the input is currently routed.
		/// </summary>
		public override bool GetInputActiveState(int input, eConnectionType type)
		{
			return input < 4 && input > 0;
		}

		/// <summary>
		/// Gets the input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override ConnectorInfo GetInput(int input)
		{
			if (input < 4 && input > 0)
				return new ConnectorInfo(input, eConnectionType.Audio | eConnectionType.Video);

			throw new ArgumentOutOfRangeException("input", "Mock conference device limited to 3 inputs");
		}

		/// <summary>
		/// Returns true if the destination contains an input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override bool ContainsInput(int input)
		{
			return input < 4 && input > 0;
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			yield return new ConnectorInfo(1, eConnectionType.Audio | eConnectionType.Video);
			yield return new ConnectorInfo(2, eConnectionType.Audio | eConnectionType.Video);
			yield return new ConnectorInfo(3, eConnectionType.Audio);
		}

		/// <summary>
		/// Sets the input address to use for the camera feed.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="cameraDeviceId"></param>
		public override void SetCameraInput(int address, int cameraDeviceId)
		{
			if (address == 1)
			{
				Parent.SetInputTypeForInput(1, eCodecInputType.Camera);
				Parent.SetInputTypeForInput(2, eCodecInputType.Content);
			}
			else if (address == 2)
			{
				Parent.SetInputTypeForInput(2, eCodecInputType.Camera);
				Parent.SetInputTypeForInput(1, eCodecInputType.Content);
			}
		}

		public override void SetContentInput(int address, int contentDeviceId)
		{
			if (address == 1)
			{
				Parent.SetInputTypeForInput(1, eCodecInputType.Content);
				Parent.SetInputTypeForInput(2, eCodecInputType.Camera);
			}
			else if (address == 2)
			{
				Parent.SetInputTypeForInput(2, eCodecInputType.Content);
				Parent.SetInputTypeForInput(1, eCodecInputType.Camera);
			}
		}

		/// <summary>
		/// Returns true if the device is actively transmitting on the given output.
		/// This is NOT the same as sending video, since some devices may send an
		/// idle signal by default.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetActiveTransmissionState(int output, eConnectionType type)
		{
			return output == 1 || output == 2;
		}

		/// <summary>
		/// Gets the output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override ConnectorInfo GetOutput(int output)
		{
			if (output == 1)
				return new ConnectorInfo(1, eConnectionType.Audio|eConnectionType.Video);
			if (output == 2)
				return new ConnectorInfo(2, eConnectionType.Audio);

			throw new ArgumentOutOfRangeException("output", "mock video conference device only supports 2 outputs");
		}

		/// <summary>
		/// Returns true if the source contains an output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override bool ContainsOutput(int output)
		{
			return output == 1 || output == 2;
		}

		/// <summary>
		/// Returns the outputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			yield return new ConnectorInfo(1, eConnectionType.Audio | eConnectionType.Video);
			yield return new ConnectorInfo(2, eConnectionType.Audio);
		}
	}
}