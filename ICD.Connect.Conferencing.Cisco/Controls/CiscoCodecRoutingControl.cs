using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cisco.Components.Presentation;
using ICD.Connect.Conferencing.Cisco.Components.Video;
using ICD.Connect.Conferencing.Cisco.Components.Video.Connectors;
using ICD.Connect.Routing;
using ICD.Connect.Routing.Connections;
using ICD.Connect.Routing.Controls;
using ICD.Connect.Routing.EventArguments;
using ICD.Connect.Routing.Utils;

namespace ICD.Connect.Conferencing.Cisco.Controls
{
	/// <summary>
	/// The CiscoCodecRoutingControl provides features for determining how content sources pass through the codec.
	/// </summary>
	public sealed class CiscoCodecRoutingControl : AbstractRouteMidpointControl<CiscoCodec>
	{
		public override event EventHandler<TransmissionStateEventArgs> OnActiveTransmissionStateChanged;
		public override event EventHandler<SourceDetectionStateChangeEventArgs> OnSourceDetectionStateChange;
		public override event EventHandler<ActiveInputStateChangeEventArgs> OnActiveInputsChanged;

		private readonly SwitcherCache m_Cache;

		#region Properties

		private VideoComponent VideoComponent { get { return Parent.Components.GetComponent<VideoComponent>(); } }

		private PresentationComponent PresentationComponent
		{
			get { return Parent.Components.GetComponent<PresentationComponent>(); }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public CiscoCodecRoutingControl(CiscoCodec parent, int id)
			: base(parent, id)
		{
			m_Cache = new SwitcherCache();
			m_Cache.OnActiveInputsChanged += CacheOnActiveInputsChanged;
			m_Cache.OnSourceDetectionStateChange += CacheOnSourceDetectionStateChange;
			m_Cache.OnActiveTransmissionStateChanged += CacheOnActiveTransmissionStateChanged;

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
			if (!VideoComponent.ContainsVideoInputConnector(address))
				throw new KeyNotFoundException(string.Format("{0} has no input at address {1}", Parent, address));

			VideoInputConnector connector = VideoComponent.GetVideoInputConnector(address);
			return new ConnectorInfo(address, connector.ConnectionType);
		}

		/// <summary>
		/// Returns the inputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetInputs()
		{
			return VideoComponent.GetVideoInputConnectorIds()
			                     .Select(id => GetInput(id));
		}

		/// <summary>
		/// Gets the input routed to the given output matching the given type.
		/// </summary>
		/// <param name="output"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">Type has multiple flags.</exception>
		public override ConnectorInfo? GetInput(int output, eConnectionType type)
		{
			return m_Cache.GetInputConnectorInfoForOutput(output, type);
		}

		/// <summary>
		/// Gets the output connector info at the given address.
		/// </summary>
		/// <param name="address"></param>
		/// <returns></returns>
		public override ConnectorInfo GetOutput(int address)
		{
			if (!VideoComponent.ContainsVideoOutputConnector(address))
				throw new KeyNotFoundException(string.Format("{0} has no output at address {1}", Parent, address));

			VideoOutputConnector connector = VideoComponent.GetVideoOutputConnector(address);
			return new ConnectorInfo(address, connector.ConnectionType);
		}

		/// <summary>
		/// Returns the outputs.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs()
		{
			return VideoComponent.GetVideoOutputConnectorIds()
			                     .Select(id => GetOutput(id));
		}

		/// <summary>
		/// Gets the outputs for the given input.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public override IEnumerable<ConnectorInfo> GetOutputs(int input, eConnectionType type)
		{
			return m_Cache.GetOutputsForInput(input, type);
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

		#endregion

		#region Component Callbacks

		/// <summary>
		/// Subscribe to the component events.
		/// </summary>
		private void SubscribeComponents()
		{
			VideoComponent.OnVideoInputConnectorConnectionStateChanged += VideoComponentOnVideoInputConnectorStateChanged;
			PresentationComponent.OnPresentationsChanged += PresentationOnPresentationsChanged;
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
			foreach (int output in GetPresentationOutputs())
			{
				int input;
				bool found = PresentationComponent.GetPresentations()
				                                  .Select(p => p.VideoInputConnector)
				                                  .TryFirst(out input);

				m_Cache.SetInputForOutput(output, found ? input : (int?)null, eConnectionType.Video);
			}
		}

		#endregion

		#region Cache Callbacks

		private void CacheOnActiveTransmissionStateChanged(object sender, TransmissionStateEventArgs args)
		{
			OnActiveTransmissionStateChanged.Raise(this, new TransmissionStateEventArgs(args.Output, args.Type, args.State));
		}

		private void CacheOnSourceDetectionStateChange(object sender, SourceDetectionStateChangeEventArgs args)
		{
			OnSourceDetectionStateChange.Raise(this, new SourceDetectionStateChangeEventArgs(args.Input, args.Type, args.State));
		}

		private void CacheOnActiveInputsChanged(object sender, ActiveInputStateChangeEventArgs args)
		{
			OnActiveInputsChanged.Raise(this, new ActiveInputStateChangeEventArgs(args.Input, args.Type, args.Active));
		}

		#endregion
	}
}
