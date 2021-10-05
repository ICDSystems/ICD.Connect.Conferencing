using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Presentation;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video.Connectors;
using ICD.Connect.Conferencing.Controls.Routing;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.RoutingGraphs;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls
{
	/// <summary>
	/// The CiscoCodecRoutingControl provides features for determining how content sources pass through the codec.
	/// </summary>
	public sealed class CiscoCodecRoutingControl : AbstractVideoConferenceRouteControl<CiscoCodecDevice>
	{
		/// <summary>
		/// Raised when the device starts/stops actively transmitting on an output.
		/// </summary>
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;

		/// <summary>
		/// Raised when an input source status changes.
		/// </summary>
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;

		/// <summary>
		/// Raised when the device starts/stops actively using an input, e.g. unroutes an input.
		/// </summary>
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;

		private IRoutingGraph m_CachedRoutingGraph;

		private readonly IcdHashSet<int> m_PresentationInputsActiveCache;
		private readonly SafeCriticalSection m_PresentationInputsActiveCacheSection;

		#region Properties

		/// <summary>
		/// Gets the routing graph.
		/// </summary>
		public IRoutingGraph RoutingGraph
		{
			get { return m_CachedRoutingGraph = m_CachedRoutingGraph ?? ServiceProvider.GetService<IRoutingGraph>(); }
		}

		/// <summary>
		/// Gets the Video Component.
		/// </summary>
		private VideoComponent VideoComponent { get { return Parent.Components.GetComponent<VideoComponent>(); } }

		/// <summary>
		/// Gets the Presentation Component.
		/// </summary>
		private PresentationComponent PresentationComponent
		{
			get { return Parent.Components.GetComponent<PresentationComponent>(); }
		}

		/// <summary>
		/// Gets the input address for the camera feed.
		/// </summary>
		public override int? CameraInput
		{
			get { return base.CameraInput; }
			protected set
			{
				int? oldValue = base.CameraInput;
				base.CameraInput = value;
				UpdateCameraInputActive(value, oldValue);
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecRoutingControl(CiscoCodecDevice parent, int id)
			: base(parent, id)
		{
			m_PresentationInputsActiveCache = new IcdHashSet<int>();
			m_PresentationInputsActiveCacheSection = new SafeCriticalSection();

			SubscribeComponents();
		}

		/// <summary>
		/// Override to release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnActiveTransmissionStateChanged = null;
			OnSourceDetectionStateChange = null;
			OnActiveInputsChanged = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the outputs for the conference display.
		/// </summary>
		public IEnumerable<int> GetMainOutputs()
		{
			return VideoComponent.GetVideoOutputConnectors()
			                     .Where(IsMainOutput)
			                     .Select(c => c.ConnectorId);
		}

		/// <summary>
		/// Gets the outputs for the self-view display.
		/// </summary>
		public IEnumerable<int> GetSelfViewOutputs()
		{
			return VideoComponent.GetVideoOutputConnectors()
			                     .Where(IsSelfViewOutput)
			                     .Select(c => c.ConnectorId);
		}

		/// <summary>
		/// Gets the outputs for the presentation display.
		/// </summary>
		public IEnumerable<int> GetPresentationOutputs()
		{
			return VideoComponent.GetVideoOutputConnectors()
			                     .Where(IsPresentationOutput)
			                     .Select(c => c.ConnectorId);
		}

		/// <summary>
		/// Sets the input address to use for the camera feed.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="cameraDeviceId"></param>
		public override void SetCameraInput(int address, int cameraDeviceId)
		{
			Parent.Components.GetComponent<VideoComponent>().SetMainVideoSourceByConnectorId(address);
		}

		/// <summary>
		/// Sets the input address to use for the content feed.
		/// </summary>
		/// <param name="address"></param>
		/// <param name="contentDeviceId"></param>
		public override void SetContentInput(int address, int contentDeviceId)
		{
		}

		/// <summary>
		/// Returns true if video is detected at the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override bool GetSignalDetectedState(int input, eConnectionType type)
		{
			// Codec may not have initialized yet
			if (!ContainsInput(input))
				return false;

			VideoInputConnector connector = VideoComponent.GetVideoInputConnector(input);
			return connector != null && connector.Connected;
		}

		/// <summary>
		/// Returns the true if the input is actively being used by the source device.
		/// For example, a display might true if the input is currently on screen,
		/// while a switcher may return true if the input is currently routed.
		/// </summary>
		public override bool GetInputActiveState(int input, eConnectionType type)
		{
			// Shortcut so camera input always returns true
			if (input == CameraInput)
				return true;

			// Check presentation inputs if it's not the camera input
			return m_PresentationInputsActiveCacheSection.Execute(() => m_PresentationInputsActiveCache.Contains(input));
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
			return true;
		}

		/// <summary>
		/// Gets the input connector info at the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public override ConnectorInfo GetInput(int address)
		{
			Connection connection = RoutingGraph.Connections.GetInputConnection(this, address);
			if (connection == null)
				throw new ArgumentOutOfRangeException("input");

			return new ConnectorInfo(connection.Destination.Address, connection.ConnectionType);
		}

		/// <summary>
		/// Returns true if the destination contains an input at the given address.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public override bool ContainsInput(int input)
		{
			return RoutingGraph.Connections.GetInputConnection(this, input) != null;
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return RoutingGraph.Connections
			                   .GetInputConnections(Parent.Id, Id)
			                   .Select(c => new ConnectorInfo(c.Destination.Address, c.ConnectionType));
		}

		/// <summary>
		/// Gets the output connector info at the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public override ConnectorInfo GetOutput(int address)
		{
			Connection connection = RoutingGraph.Connections.GetOutputConnection(this, address);
			if (connection == null)
				throw new ArgumentOutOfRangeException("address");

			return new ConnectorInfo(connection.Source.Address, connection.ConnectionType);
		}

		/// <summary>
		/// Returns true if the source contains an output at the given address.
		/// </summary>
		/// <param name="output"></param>
		/// <returns></returns>
		public override bool ContainsOutput(int output)
		{
			return RoutingGraph.Connections.GetOutputConnection(this, output) != null;
		}

		/// <summary>
		/// Returns the outputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			return RoutingGraph.Connections
			                   .GetOutputConnections(Parent.Id, Id)
			                   .Select(c => new ConnectorInfo(c.Source.Address, c.ConnectionType));
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Returns true if the given connector is being used for the main display.
		/// </summary>
		/// <param name="connector"></param>
		/// <returns></returns>
		private bool IsMainOutput(VideoOutputConnector connector)
		{
			eMonitors monitors = VideoComponent.Monitors;

			switch (connector.MonitorRole)
			{
				case VideoOutputConnector.eMonitorRole.PresentationOnly:
					return false;

				case VideoOutputConnector.eMonitorRole.Auto:
				case VideoOutputConnector.eMonitorRole.Recorder:
				case VideoOutputConnector.eMonitorRole.First:
					return true;

				case VideoOutputConnector.eMonitorRole.Second:
					return monitors == eMonitors.Triple ||
					       monitors == eMonitors.TriplePresentationOnly ||
					       monitors == eMonitors.Quadruple;

				case VideoOutputConnector.eMonitorRole.Third:
					return monitors == eMonitors.Quadruple;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Returns true if the given connector is being used for selfview.
		/// </summary>
		/// <param name="connector"></param>
		/// <returns></returns>
		private bool IsSelfViewOutput(VideoOutputConnector connector)
		{
			eSelfViewMonitorRole role = VideoComponent.SelfViewMonitor;

			switch (connector.MonitorRole)
			{
				case VideoOutputConnector.eMonitorRole.Auto:
				case VideoOutputConnector.eMonitorRole.Recorder:
					return !VideoComponent.SelfViewFullScreenEnabled;

				case VideoOutputConnector.eMonitorRole.PresentationOnly:
					return false;

				case VideoOutputConnector.eMonitorRole.First:
					return role == eSelfViewMonitorRole.First;
				case VideoOutputConnector.eMonitorRole.Second:
					return role == eSelfViewMonitorRole.Second;
				case VideoOutputConnector.eMonitorRole.Third:
					return role == eSelfViewMonitorRole.Third;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Returns true if the given connector is being used for presentations.
		/// </summary>
		/// <param name="connector"></param>
		/// <returns></returns>
		private bool IsPresentationOutput(VideoOutputConnector connector)
		{
			eMonitors monitors = VideoComponent.Monitors;

			switch (connector.MonitorRole)
			{
				case VideoOutputConnector.eMonitorRole.Auto:
				case VideoOutputConnector.eMonitorRole.PresentationOnly:
				case VideoOutputConnector.eMonitorRole.Recorder:
					return true;

				case VideoOutputConnector.eMonitorRole.First:
					return monitors == eMonitors.Single;
				case VideoOutputConnector.eMonitorRole.Second:
					return monitors == eMonitors.Dual || monitors == eMonitors.DualPresentationOnly;
				case VideoOutputConnector.eMonitorRole.Third:
					return monitors == eMonitors.Triple || monitors == eMonitors.TriplePresentationOnly;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/// <summary>
		/// Sets the presentation input state
		/// Does not raise event if the input is also being used as the camera input
		/// </summary>
		/// <param name="input"></param>
		/// <param name="state"></param>
		private void SetPresentationInputActiveState(int input, bool state)
		{
			m_PresentationInputsActiveCacheSection.Enter();
			try
			{
				if (state == m_PresentationInputsActiveCache.Contains(input))
					return;

				if (state)
					m_PresentationInputsActiveCache.Add(input);
				else
					m_PresentationInputsActiveCache.Remove(input);
			}
			finally
			{
				m_PresentationInputsActiveCacheSection.Leave();
			}

			// Don't raise the event if this is also the camera input
			if (input != CameraInput)
				OnActiveInputsChanged.Raise(this, new ActiveInputStateChangeEventArgs(input, eConnectionType.Audio | eConnectionType.Video, state));
		}

		/// <summary>
		/// Sets the given inputs as the active presentation sources
		/// Any inputs not passed in are set as inactive
		/// Does not raise events if the inputs are also being used as the camera input
		/// </summary>
		/// <param name="activeInputs"></param>
		private void SetPresentationInputActiveState(IEnumerable<int> activeInputs)
		{
			int[] newInputs = activeInputs.ToArray();

			List<int> removedValues;
			List<int> addedValues = new List<int>();

			m_PresentationInputsActiveCacheSection.Enter();
			try
			{
				removedValues = m_PresentationInputsActiveCache.Except(newInputs).ToList();

				foreach (int input in removedValues)
					m_PresentationInputsActiveCache.Remove(input);

				addedValues.AddRange(newInputs.Where(input => m_PresentationInputsActiveCache.Add(input)));
			}
			finally
			{
				m_PresentationInputsActiveCacheSection.Leave();
			}

			foreach (int input in removedValues.Where(input => input != CameraInput))
				OnActiveInputsChanged.Raise(this,
				                            new ActiveInputStateChangeEventArgs(input, eConnectionType.Audio | eConnectionType.Video,
				                                                                false));

			foreach (int input in addedValues.Where(input => input != CameraInput))
				OnActiveInputsChanged.Raise(this,
				                            new ActiveInputStateChangeEventArgs(input, eConnectionType.Audio | eConnectionType.Video,
				                                                                true));
		}

		/// <summary>
		/// Raises events for udating the active camera input
		/// Doesn't raise events if the inputs are being used for presentations
		/// </summary>
		/// <param name="newInput"></param>
		/// <param name="oldInput"></param>
		private void UpdateCameraInputActive(int? newInput, int? oldInput)
		{
			if (newInput == oldInput)
				return;

			// Check if the inputs are already used by presentation
			// If they are, we won't raise events for those inputs
			bool oldInputEvent;
			bool newInputEvent;

			m_PresentationInputsActiveCacheSection.Enter();
			try
			{
				oldInputEvent = oldInput.HasValue && !m_PresentationInputsActiveCache.Contains(oldInput.Value);
				newInputEvent = newInput.HasValue && !m_PresentationInputsActiveCache.Contains(newInput.Value);
			}
			finally
			{
				m_PresentationInputsActiveCacheSection.Leave();
			}

			if (oldInputEvent)
				OnActiveInputsChanged.Raise(this,
											new ActiveInputStateChangeEventArgs(oldInput.Value, eConnectionType.Audio | eConnectionType.Video,
																				false));

			if (newInputEvent)
				OnActiveInputsChanged.Raise(this,
											new ActiveInputStateChangeEventArgs(newInput.Value, eConnectionType.Audio | eConnectionType.Video,
																				true));
		}

		#endregion

		#region Component Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		private void SubscribeComponents()
		{
			VideoComponent.OnVideoInputConnectorConnectionStateChanged += VideoComponentOnVideoInputConnectorStateChanged;
			VideoComponent.OnMainVideoSourceChanged += VideoComponentOnMainVideoSourceChanged;
			PresentationComponent.OnPresentationsChanged += PresentationOnPresentationsChanged;
		}

		private void VideoComponentOnMainVideoSourceChanged(object sender, IntEventArgs intEventArgs)
		{
			CameraInput = intEventArgs.Data;
		}

		/// <summary>
		/// Raised when the video detection state changes at an input.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void VideoComponentOnVideoInputConnectorStateChanged(object sender, VideoConnectionStateEventArgs eventArgs)
		{
			VideoInputConnector connector = sender as VideoInputConnector;
			if (connector == null)
				return;

			SourceDetectionStateChangeEventArgs args =
				new SourceDetectionStateChangeEventArgs(connector.ConnectorId,
				                                        connector.ConnectionType,
				                                        eventArgs.State);

			OnSourceDetectionStateChange.Raise(this, args);
		}

		/// <summary>
		/// Called when a presentation stops or starts.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void PresentationOnPresentationsChanged(object sender, EventArgs eventArgs)
		{
			SetPresentationInputActiveState(PresentationComponent.GetPresentations().Select(p => p.VideoInputConnector));
		}

		#endregion
	}
}
