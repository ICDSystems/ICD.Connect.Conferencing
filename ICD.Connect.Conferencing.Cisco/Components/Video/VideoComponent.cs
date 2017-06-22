﻿using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Components.Video.Connectors;

namespace ICD.Connect.Conferencing.Cisco.Components.Video
{
	public enum eLayoutTarget
	{
		[UsedImplicitly] Local,
		[UsedImplicitly] Remote
	}

	public enum eLayoutFamily
	{
		[UsedImplicitly] Auto,
		[UsedImplicitly] Custom,
		[UsedImplicitly] Equal,
		[UsedImplicitly] Overlay,
		[UsedImplicitly] Prominent,
		[UsedImplicitly] Single
	}

	/// <summary>
	/// VideoComponent provides functionality for controlling the codec video features.
	/// </summary>
	public sealed class VideoComponent : AbstractCiscoComponent
	{
		/// <summary>
		/// Raised when self-view becomes enabled or disabled.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnSelfViewEnabledChanged;

		/// <summary>
		/// Raised when self-view becomes fullscreen or windowed.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnSelfViewFullScreenEnabledChanged;

		/// <summary>
		/// Raised when the self-view position changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<PipPositionEventArgs> OnSelfViewPositionChanged;

		/// <summary>
		/// Raised when the self-view monitor changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<SelfViewMonitorRoleEventArgs> OnSelfViewMonitorChanged;

		/// <summary>
		/// Raised when the active speaker position changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<PipPositionEventArgs> OnActiveSpeakerPositionChanged;

		/// <summary>
		/// Raised when the main video source changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<IntEventArgs> OnMainVideoSourceChanged;

		/// <summary>
		/// Raised when a video input connector connection state changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<VideoConnectionStateEventArgs> OnVideoInputConnectorConnectionStateChanged;

		/// <summary>
		/// Called when the number of monitors changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<MonitorsEventArgs> OnMonitorsChanged; 

		private bool m_SelfViewEnabled;
		private bool m_SelfViewFullScreenEnabled;
		private ePipPosition m_SelfViewPosition;
		private eSelfViewMonitorRole m_SelfViewMonitor;
		private ePipPosition m_ActiveSpeakerPosition;
		private int m_MainVideoSource;
		private eMonitors m_Monitors;

		private readonly Dictionary<int, VideoSource> m_VideoSources;
		private readonly Dictionary<int, VideoInputConnector> m_VideoInputConnectors;
		private readonly Dictionary<int, VideoOutputConnector> m_VideoOutputConnectors;

		#region Properties

		/// <summary>
		/// Gets the number of input connections.
		/// </summary>
		public int VideoInputConnectorCount { get { return m_VideoInputConnectors.Count; } }

		/// <summary>
		/// Gets the number of output connections.
		/// </summary>
		public int VideoOutputConnectorCount { get { return m_VideoOutputConnectors.Count; } }

		/// <summary>
		/// Gets the current enabled state of self-view.
		/// </summary>
		public bool SelfViewEnabled
		{
			get { return m_SelfViewEnabled; }
			private set
			{
				if (value == m_SelfViewEnabled)
					return;

				m_SelfViewEnabled = value;

				Codec.Log(eSeverity.Informational, "Selfview is {0}", m_SelfViewEnabled ? "On" : "Off");

				OnSelfViewEnabledChanged.Raise(this, new BoolEventArgs(m_SelfViewEnabled));
			}
		}

		/// <summary>
		/// Gets the current fullscreen enabled state of self-view.
		/// </summary>
		public bool SelfViewFullScreenEnabled
		{
			get { return m_SelfViewFullScreenEnabled; }
			private set
			{
				if (value == m_SelfViewFullScreenEnabled)
					return;

				m_SelfViewFullScreenEnabled = value;

				Codec.Log(eSeverity.Informational, "Selfview Fullscreen is {0}", m_SelfViewFullScreenEnabled ? "On" : "Off");

				OnSelfViewFullScreenEnabledChanged.Raise(this, new BoolEventArgs(m_SelfViewFullScreenEnabled));
			}
		}

		/// <summary>
		/// Gets the current self-view position.
		/// </summary>
		[PublicAPI]
		public ePipPosition SelfViewPosition
		{
			get { return m_SelfViewPosition; }
			private set
			{
				if (value == m_SelfViewPosition)
					return;

				m_SelfViewPosition = value;

				Codec.Log(eSeverity.Informational, "Selfview PIP Position is {0}", StringUtils.NiceName(m_SelfViewPosition));

				OnSelfViewPositionChanged.Raise(null, new PipPositionEventArgs(m_SelfViewPosition));
			}
		}

		/// <summary>
		/// Gets the current self-view monitor.
		/// </summary>
		[PublicAPI]
		public eSelfViewMonitorRole SelfViewMonitor
		{
			get { return m_SelfViewMonitor; }
			private set
			{
				if (value == m_SelfViewMonitor)
					return;

				m_SelfViewMonitor = value;

				Codec.Log(eSeverity.Informational, "Selfview is on Monitor {0}", m_SelfViewMonitor);

				OnSelfViewMonitorChanged.Raise(this, new SelfViewMonitorRoleEventArgs(m_SelfViewMonitor));
			}
		}

		/// <summary>
		/// Gets the active speaker position.
		/// </summary>
		[PublicAPI]
		public ePipPosition ActiveSpeakerPosition
		{
			get { return m_ActiveSpeakerPosition; }
			private set
			{
				if (value == m_ActiveSpeakerPosition)
					return;

				m_ActiveSpeakerPosition = value;

				Codec.Log(eSeverity.Informational, "Active Speaker Position is {0}", StringUtils.NiceName(m_ActiveSpeakerPosition));

				OnActiveSpeakerPositionChanged.Raise(this, new PipPositionEventArgs(m_ActiveSpeakerPosition));
			}
		}

		/// <summary>
		/// Gets the main video source.
		/// </summary>
		[PublicAPI]
		public int MainVideoSource
		{
			get { return m_MainVideoSource; }
			private set
			{
				if (value == m_MainVideoSource)
					return;

				m_MainVideoSource = value;

				Codec.Log(eSeverity.Informational, "Near End Input {0} is Selected", m_MainVideoSource);

				OnMainVideoSourceChanged.Raise(this, new IntEventArgs(m_MainVideoSource));
			}
		}

		/// <summary>
		/// Gets the number of monitors and their configuration.
		/// </summary>
		[PublicAPI]
		public eMonitors Monitors
		{
			get { return m_Monitors; }
			private set
			{
				if (value == m_Monitors)
					return;

				m_Monitors = value;

				Codec.Log(eSeverity.Informational, "Monitors are in {0} configuration", StringUtils.NiceName(m_Monitors));

				OnMonitorsChanged.Raise(this, new MonitorsEventArgs(m_Monitors));
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public VideoComponent(CiscoCodec codec) : base(codec)
		{
			m_VideoSources = new Dictionary<int, VideoSource>();
			m_VideoInputConnectors = new Dictionary<int, VideoInputConnector>();
			m_VideoOutputConnectors = new Dictionary<int, VideoOutputConnector>();

			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		public override void Dispose()
		{
			OnSelfViewEnabledChanged = null;
			OnSelfViewFullScreenEnabledChanged = null;
			OnSelfViewPositionChanged = null;
			OnSelfViewMonitorChanged = null;
			OnActiveSpeakerPositionChanged = null;
			OnMainVideoSourceChanged = null;
			OnVideoInputConnectorConnectionStateChanged = null;
			OnMonitorsChanged = null;

			base.Dispose();

			foreach (VideoInputConnector connector in m_VideoInputConnectors.Values)
				Unsubscribe(connector);
		}

		/// <summary>
		/// Sets the video layout family.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="layout"></param>
		public void SetLayout(eLayoutTarget target, eLayoutFamily layout)
		{
			Codec.SendCommand("xCommand Video Layout LayoutFamily Set Target: {0} LayoutFamily: {1}", target, layout);
			Codec.Log(eSeverity.Informational, "Setting {0} Layout to {1}", target, layout);
		}

		/// <summary>
		/// Sets self-view enabled state.
		/// </summary>
		public void SetSelfViewEnabled(bool state)
		{
			string on = (state) ? "On" : "Off";

			Codec.SendCommand("xCommand Video Selfview Set Mode: {0}", on);
			Codec.Log(eSeverity.Informational, "Setting Selfview " + on);
		}

		/// <summary>
		/// Sets self-view Picture-in-Picture position.
		/// </summary>
		/// <param name="position"></param>
		[PublicAPI]
		public void SetSelfViewPosition(ePipPosition position)
		{
			Codec.SendCommand("xCommand Video Selfview Set PIPPosition: {0}", position);
			Codec.Log(eSeverity.Informational, "Setting Selfview PIP to {0}", StringUtils.NiceName(position));
		}

		/// <summary>
		/// Sets the self-view fullscreen state.
		/// </summary>
		/// <param name="state"></param>
		public void SetSelfViewFullScreen(bool state)
		{
			string on = (state) ? "On" : "Off";

			Codec.SendCommand("xCommand Video Selfview Set FullscreenMode: {0}", on);
			Codec.Log(eSeverity.Informational, "Setting Selfview Fullscreen {0}", on);
		}

		/// <summary>
		/// Sets the self-view monitor.
		/// </summary>
		/// <param name="monitor"></param>
		[PublicAPI]
		public void SetSelfViewMonitor(eSelfViewMonitorRole monitor)
		{
			Codec.SendCommand("xCommand Video Selfview Set OnMonitorRole: {0}", monitor);
			Codec.Log(eSeverity.Informational, "Setting Selfview to show on Monitor {0}", (int)monitor);
		}

		/// <summary>
		/// Sets the Picture-in-Picture position for the active speaker.
		/// </summary>
		/// <param name="position"></param>
		[PublicAPI]
		public void SetActiveSpeakerPosition(ePipPosition position)
		{
			Codec.SendCommand("xCommand Video PIP ActiveSpeaker Set Position: {0}", position);
			Codec.Log(eSeverity.Informational, "Setting Active Speaker PIP to {0}", StringUtils.NiceName(position));
		}

		/// <summary>
		/// Sets the main video source.
		/// </summary>
		/// <param name="sourceId"></param>
		[PublicAPI]
		public void SetMainVideoSource(int sourceId)
		{
			Codec.SendCommand("xCommand Video Input SetMainVideoSource SourceId: {0}", sourceId);
			Codec.Log(eSeverity.Informational, "Setting Main Video Source SourceId: {0}", sourceId);
		}

		/// <summary>
		/// Sets the active video connector.
		/// </summary>
		/// <param name="connectorId"></param>
		[PublicAPI]
		public void SetActiveVideoConnector(int connectorId)
		{
			Codec.SendCommand("xCommand Video Input Source SetActiveConnector ConnectorId: {0}", connectorId);
			Codec.Log(eSeverity.Informational, "Setting Active Video Input Connector: {0}", connectorId);
		}

		/// <summary>
		/// Gets an array of video input connectors for the given connector type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public IEnumerable<VideoInputConnector> GetVideoInputConnectorsByType(eConnectorType type)
		{
			return m_VideoInputConnectors.Values.Where(c => c.ConnectorType == type).ToArray();
		}

		/// <summary>
		/// Returns true if a video input connector with the given id exists.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool ContainsVideoInputConnector(int id)
		{
			return m_VideoInputConnectors.ContainsKey(id);
		}

		/// <summary>
		/// Gets the video input connector with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public VideoInputConnector GetVideoInputConnector(int id)
		{
			if (!ContainsVideoInputConnector(id))
				throw new KeyNotFoundException(string.Format("{0} contains no {1} with id {2}", GetType().Name,
				                                             typeof(VideoInputConnector).Name, id));
			return m_VideoInputConnectors[id];
		}

		/// <summary>
		/// Gets the input input video connectors.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<VideoInputConnector> GetVideoInputConnectors()
		{
			return m_VideoInputConnectors.Values.ToArray();
		}

		/// <summary>
		/// Returns true if a video output connector with the given id exists.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool ContainsVideoOutputConnector(int id)
		{
			return m_VideoOutputConnectors.ContainsKey(id);
		}

		/// <summary>
		/// Gets the video output connector with the given id.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public VideoOutputConnector GetVideoOutputConnector(int id)
		{
			if (!ContainsVideoOutputConnector(id))
				throw new KeyNotFoundException(string.Format("{0} contains no {1} with id {2}", GetType().Name,
				                                             typeof(VideoOutputConnector).Name, id));
			return m_VideoOutputConnectors[id];
		}

		/// <summary>
		/// Gets the output video connectors.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public IEnumerable<VideoOutputConnector> GetVideoOutputConnectors()
		{
			return m_VideoOutputConnectors.Values.ToArray();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodec codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseSelfViewStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Selfview", "Mode");
			codec.RegisterParserCallback(ParseSelfViewPositionStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Selfview",
			                             "PIPPosition");
			codec.RegisterParserCallback(ParseSelfViewFullscreenStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Selfview",
			                             "FullscreenMode");
			codec.RegisterParserCallback(ParseSelfViewMonitorStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Selfview",
			                             "OnMonitorRole");
			codec.RegisterParserCallback(ParseActiveSpeakerPositionStatus, CiscoCodec.XSTATUS_ELEMENT, "Video",
			                             "ActiveSpeaker", "PIPPosition");
			codec.RegisterParserCallback(ParseVideoSourceStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Input", "Source");
			codec.RegisterParserCallback(ParseVideoInputConnectorStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Input",
			                             "Connector");
			codec.RegisterParserCallback(ParseVideoOutputConnectorStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Output",
			                             "Connector");
			codec.RegisterParserCallback(ParseMainVideoSourceStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Input",
			                             "MainVideoSource");
			codec.RegisterParserCallback(ParseMonitors, CiscoCodec.XSTATUS_ELEMENT, "Video", "Monitors");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodec codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseSelfViewStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Selfview", "Mode");
			codec.UnregisterParserCallback(ParseSelfViewPositionStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Selfview",
			                               "PIPPosition");
			codec.UnregisterParserCallback(ParseSelfViewFullscreenStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Selfview",
			                               "FullscreenMode");
			codec.UnregisterParserCallback(ParseSelfViewMonitorStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Selfview",
			                               "OnMonitorRole");
			codec.UnregisterParserCallback(ParseActiveSpeakerPositionStatus, CiscoCodec.XSTATUS_ELEMENT, "Video",
			                               "ActiveSpeaker", "PIPPosition");
			codec.UnregisterParserCallback(ParseVideoSourceStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Input", "Source");
			codec.UnregisterParserCallback(ParseVideoInputConnectorStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Input",
			                               "Connector");
			codec.UnregisterParserCallback(ParseVideoOutputConnectorStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Output",
			                               "Connector");
			codec.UnregisterParserCallback(ParseMainVideoSourceStatus, CiscoCodec.XSTATUS_ELEMENT, "Video", "Input",
			                               "MainVideoSource");
			codec.UnregisterParserCallback(ParseMonitors, CiscoCodec.XSTATUS_ELEMENT, "Video", "Monitors");
		}

		private void ParseMonitors(CiscoCodec codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			Monitors = EnumUtils.Parse<eMonitors>(content, true);
		}

		private void ParseSelfViewStatus(CiscoCodec sender, string resultId, string xml)
		{
			SelfViewEnabled = XmlUtils.GetInnerXml(xml) == "On";
		}

		private void ParseSelfViewPositionStatus(CiscoCodec sender, string resultId, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			SelfViewPosition = EnumUtils.Parse<ePipPosition>(content, true);
		}

		private void ParseSelfViewFullscreenStatus(CiscoCodec sender, string resultId, string xml)
		{
			SelfViewFullScreenEnabled = XmlUtils.GetInnerXml(xml) == "On";
		}

		private void ParseSelfViewMonitorStatus(CiscoCodec sender, string resultId, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			SelfViewMonitor = EnumUtils.Parse<eSelfViewMonitorRole>(content, true);
		}

		private void ParseActiveSpeakerPositionStatus(CiscoCodec sender, string resultId, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			ActiveSpeakerPosition = EnumUtils.Parse<ePipPosition>(content, true);
		}

		private void ParseVideoSourceStatus(CiscoCodec sender, string resultId, string xml)
		{
			VideoSource source = VideoSource.FromXml(xml);
			int sourceId = source.SourceId;

			if (m_VideoSources.ContainsKey(sourceId))
				m_VideoSources[sourceId].UpdateFromXml(xml);
			else
				m_VideoSources[sourceId] = source;
		}

		private void ParseVideoInputConnectorStatus(CiscoCodec sender, string resultId, string xml)
		{
			int connectorId = AbstractVideoConnector.GetConnectorId(xml);
			VideoInputConnector connector = m_VideoInputConnectors.GetDefault(connectorId, null);

			// Create the new connector.
			if (connector == null)
			{
				connector = new VideoInputConnector();
				m_VideoInputConnectors[connectorId] = connector;
				Subscribe(connector);
			}

			// Update
			connector.UpdateFromXml(xml);
		}

		private void ParseVideoOutputConnectorStatus(CiscoCodec sender, string resultId, string xml)
		{
			int connectorId = AbstractVideoConnector.GetConnectorId(xml);
			VideoOutputConnector connector = m_VideoOutputConnectors.GetDefault(connectorId, null);

			// Create the new connector.
			if (connector == null)
			{
				connector = new VideoOutputConnector();
				m_VideoOutputConnectors[connectorId] = connector;
			}

			// Update
			connector.UpdateFromXml(xml);
		}

		private void ParseMainVideoSourceStatus(CiscoCodec sender, string resultId, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			MainVideoSource = int.Parse(content);
		}

		#endregion

		#region Video Input Connector Callbacks

		/// <summary>
		/// Subscribe to the connector events.
		/// </summary>
		/// <param name="connector"></param>
		private void Subscribe(VideoInputConnector connector)
		{
			if (connector == null)
				return;

			connector.OnConnectedStateChanged += ConnectorOnConnectedStateChanged;
		}

		/// <summary>
		/// Unsubscribe from the connector events.
		/// </summary>
		/// <param name="connector"></param>
		private void Unsubscribe(VideoInputConnector connector)
		{
			if (connector == null)
				return;

			connector.OnConnectedStateChanged -= ConnectorOnConnectedStateChanged;
		}

		/// <summary>
		/// Called when a connector connection state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void ConnectorOnConnectedStateChanged(object sender, BoolEventArgs boolEventArgs)
		{
			VideoInputConnector connector = sender as VideoInputConnector;
			if (connector == null)
				throw new ArgumentException();

			int id = connector.ConnectorId;
			bool state = boolEventArgs.Data;

			OnVideoInputConnectorConnectionStateChanged.Raise(this, new VideoConnectionStateEventArgs(id, state));
		}

		#endregion
	}
}
