using System;
using System.Collections.Generic;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;

namespace ICD.Connect.Conferencing.Controls.Routing
{
	public abstract class AbstractVideoConferenceRouteControl<TParent> :
		AbstractRouteDestinationControl<TParent>, IVideoConferenceRouteControl
		where TParent : IVideoConferenceDevice
	{
		/// <summary>
		/// Raised when the device starts/stops actively transmitting on an output.
		/// </summary>
		public abstract event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		/// <summary>
		/// Raised when the camera input changes.
		/// </summary>
		public event EventHandler<ConferenceRouteDestinationCameraInputApiEventArgs> OnCameraInputChanged;

		private int? m_CameraInput;

		/// <summary>
		/// Gets the input address for the camera feed.
		/// </summary>
		public int? CameraInput
		{
			get { return m_CameraInput; }
			protected set
			{
				if (value == m_CameraInput)
					return;

				m_CameraInput = value;

				Logger.LogSetTo(eSeverity.Informational, "Camera Input", m_CameraInput);

				OnCameraInputChanged.Raise(this, new ConferenceRouteDestinationCameraInputApiEventArgs(m_CameraInput));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		protected AbstractVideoConferenceRouteControl(TParent parent, int id)
			: base(parent, id)
		{
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnCameraInputChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the codec input type for the input with the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public eCodecInputType GetCodecInputType(int address)
		{
			return Parent.InputTypes.GetInputType(address);
		}

		/// <summary>
		/// Gets the input addresses with the given codec input type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<int> GetCodecInputs(eCodecInputType type)
		{
			return Parent.InputTypes.GetInputs(type);
		}

		/// <summary>
		/// Sets the input address to use for the camera feed.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="cameraDeviceId"></param>
		public abstract void SetCameraInput(int address, int cameraDeviceId);

		/// <summary>
		/// Returns true if the device is actively transmitting on the given output.
		/// This is NOT the same as sending video, since some devices may send an
		/// idle signal by default.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public abstract bool GetActiveTransmissionState(int output, eConnectionType type);

		/// <summary>
		/// Gets the output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public abstract ConnectorInfo GetOutput(int output);

		/// <summary>
		/// Returns true if the source contains an output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public abstract bool ContainsOutput(int output);

		/// <summary>
		/// Returns the outputs.
		/// </summary>
		/// <returns></returns>
		public abstract IEnumerable<ConnectorInfo> GetOutputs();

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (IConsoleNodeBase node in VideoConferenceRouteDestinationControlConsole.GetConsoleNodes(this))
				yield return node;
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			VideoConferenceRouteDestinationControlConsole.BuildConsoleStatus(this, addRow);
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			foreach (IConsoleCommand command in VideoConferenceRouteDestinationControlConsole.GetConsoleCommands(this))
				yield return command;
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		#endregion
	}
}
