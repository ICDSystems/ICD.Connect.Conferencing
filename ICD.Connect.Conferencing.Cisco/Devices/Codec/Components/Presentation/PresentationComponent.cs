using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Presentation
{
	/// <summary>
	/// PresentationComponent provides functionality for controlling presentations.
	/// </summary>
	public sealed class PresentationComponent : AbstractCiscoComponent
	{
		/// <summary>
		/// Called when the presentation position changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<PipPositionEventArgs> OnPresentationPositionChanged;

		/// <summary>
		/// Called when the presentation view changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<LayoutEventArgs> OnPresentationViewChanged;

		/// <summary>
		/// Called when the presentation mode changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<PresentationModeEventArgs> OnPresentationModeChanged;

		/// <summary>
		/// Called when one or more presentations are added or removed.
		/// </summary>
		public event EventHandler OnPresentationsChanged;

		/// <summary>
		/// Called when the presentation stops.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnPresentationStopped;

		private readonly Dictionary<int, PresentationItem> m_Presentations;
		private readonly SafeCriticalSection m_PresentationsSection;

		private ePipPosition m_PresentationPosition;
		private eLayout m_PresentationView;
		private ePresentationMode m_PresentationMode;

		#region Properties

		/// <summary>
		/// Gets the presentation position.
		/// </summary>
		[PublicAPI]
		public ePipPosition PresentationPosition
		{
			get { return m_PresentationPosition; }
			private set
			{
				if (value == m_PresentationPosition)
					return;

				m_PresentationPosition = value;

				Codec.Log(eSeverity.Informational, "Presentation position set to: {0}", StringUtils.NiceName(m_PresentationPosition));

				OnPresentationPositionChanged.Raise(this, new PipPositionEventArgs(m_PresentationPosition));
			}
		}

		/// <summary>
		/// Gets the presentation view.
		/// </summary>
		[PublicAPI]
		public eLayout PresentationView
		{
			get { return m_PresentationView; }
			private set
			{
				if (value == m_PresentationView)
					return;

				m_PresentationView = value;

				Codec.Log(eSeverity.Informational, "Presentation view set to: {0}", StringUtils.NiceName(m_PresentationView));

				OnPresentationViewChanged.Raise(this, new LayoutEventArgs(m_PresentationView));
			}
		}

		/// <summary>
		/// Gets the current presentation mode.
		/// </summary>
		[PublicAPI]
		public ePresentationMode PresentationMode
		{
			get { return m_PresentationMode; }
			private set
			{
				if (value == m_PresentationMode)
					return;

				m_PresentationMode = value;

				Codec.Log(eSeverity.Informational, "Presentation mode set to: {0}", StringUtils.NiceName(m_PresentationMode));

				OnPresentationModeChanged.Raise(this, new PresentationModeEventArgs(m_PresentationMode));
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public PresentationComponent(CiscoCodecDevice codec) : base(codec)
		{
			m_Presentations = new Dictionary<int, PresentationItem>();
			m_PresentationsSection = new SafeCriticalSection();

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
			OnPresentationPositionChanged = null;
			OnPresentationViewChanged = null;
			OnPresentationsChanged = null;
			OnPresentationStopped = null;

			base.Dispose();
		}

		/// <summary>
		/// Starts the presentation.
		/// </summary>
		/// <param name="presentation"></param>
		[PublicAPI]
		public void StartPresentation(PresentationItem presentation)
		{
			StartPresentation(presentation.VideoInputConnector, presentation.SendingMode);
		}

		/// <summary>
		/// Starts a presentation.
		/// </summary>
		/// <param name="sourceId"></param>
		/// <param name="mode"></param>
		[PublicAPI]
		public void StartPresentation(int sourceId, PresentationItem.eSendingMode mode)
		{
			if (mode == PresentationItem.eSendingMode.Off)
			{
				StopPresentation(sourceId);
				return;
			}

			Codec.SendCommand("xCommand Presentation Start PresentationSource: {0} SendingMode: {1}", sourceId, mode);
			Codec.Log(eSeverity.Informational, "Starting {0} Presentation from Input {1}", StringUtils.NiceName(mode), sourceId);
		}

		/// <summary>
		/// Stops the presentation.
		/// </summary>
		/// <param name="presentation"></param>
		[PublicAPI]
		public void StopPresentation(PresentationItem presentation)
		{
			StopPresentation(presentation.VideoInputConnector);
		}

		/// <summary>
		/// Stops the presentation.
		/// </summary>
		/// <param name="sourceId"></param>
		[PublicAPI]
		public void StopPresentation(int sourceId)
		{
			Codec.SendCommand("xCommand Presentation Stop PresentationSource: {0}", sourceId);
			Codec.Log(eSeverity.Informational, "End Presentation");
		}

		/// <summary>
		/// Sets the Picture-in-Picture position for the presentation.
		/// </summary>
		/// <param name="position"></param>
		[PublicAPI]
		public void SetPresentationPosition(ePipPosition position)
		{
			Codec.SendCommand("xCommand Video PresentationPIP Set Position: {0}", position);
			Codec.Log(eSeverity.Informational, "Setting presentation PIP Position: {0}", position);
		}

		/// <summary>
		/// Sets the presentation view layout.
		/// </summary>
		/// <param name="layout"></param>
		[PublicAPI]
		public void SetPresentationView(eLayout layout)
		{
			Codec.SendCommand("xCommand Video Layout SetPresentationView View: {0}", layout);
			Codec.Log(eSeverity.Informational, "Setting presentation View: {0}", layout);
		}

		/// <summary>
		/// Gets the current presentations.
		/// </summary>
		/// <returns></returns>
		[PublicAPI]
		public PresentationItem[] GetPresentations()
		{
			return m_PresentationsSection.Execute(() => m_Presentations.Values.ToArray());
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParsePresentationPositionStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Video",
			                             "Presentation", "PIPPosition");
			codec.RegisterParserCallback(ParsePresentationViewStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Video", "Layout",
			                             "PresentationView");
			codec.RegisterParserCallback(ParsePresentation, CiscoCodecDevice.XSTATUS_ELEMENT, "Conference",
			                             "Presentation");
			codec.RegisterParserCallback(ParsePresentationMode, CiscoCodecDevice.XSTATUS_ELEMENT, "Conference",
			                             "Presentation", "Mode");
			codec.RegisterParserCallback(ParsePresentationStoppedCauseEvent, CiscoCodecDevice.XEVENT_ELEMENT, "PresentationStopped",
			                             "Cause");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParsePresentationPositionStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Video",
			                               "Presentation", "PIPPosition");
			codec.UnregisterParserCallback(ParsePresentationViewStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Video", "Layout",
			                               "PresentationView");
			codec.UnregisterParserCallback(ParsePresentation, CiscoCodecDevice.XSTATUS_ELEMENT, "Conference",
			                               "Presentation");
			codec.UnregisterParserCallback(ParsePresentationMode, CiscoCodecDevice.XSTATUS_ELEMENT, "Conference",
			                               "Presentation", "Mode");
			codec.UnregisterParserCallback(ParsePresentationStoppedCauseEvent, CiscoCodecDevice.XEVENT_ELEMENT, "PresentationStopped",
			                               "Cause");
		}

		private void ParsePresentationMode(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			PresentationMode = EnumUtils.Parse<ePresentationMode>(content, true);
		}

		private void ParsePresentationPositionStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			PresentationPosition = EnumUtils.Parse<ePipPosition>(content, true);
		}

		private void ParsePresentationViewStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			PresentationView = EnumUtils.Parse<eLayout>(content, true);
		}

		private void ParsePresentation(CiscoCodecDevice sender, string resultId, string xml)
		{
			bool changed = false;

			m_PresentationsSection.Enter();

			try
			{
				using (IcdXmlReader reader = new IcdXmlReader(xml))
				{
					reader.ReadToNextElement();

					foreach (IcdXmlReader localInstance in reader.GetChildElements().Where(l => l.Name == "LocalInstance"))
					{
						int item = localInstance.GetAttributeAsInt("item");
						bool ghost = localInstance.HasAttribute("ghost") && localInstance.GetAttributeAsBool("ghost");

						if (ghost)
							m_Presentations.Remove(item);
						else
							m_Presentations[item] = PresentationItem.FromLocalInstance(localInstance);

						changed = true;

						localInstance.Dispose();
					}
				}
			}
			finally
			{
				m_PresentationsSection.Leave();
			}

			if (changed)
				OnPresentationsChanged.Raise(this);
		}

		private void ParsePresentationStoppedCauseEvent(CiscoCodecDevice sender, string resultId, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);

			Codec.Log(eSeverity.Informational, "Presentation stopped: {0}", content);

			OnPresentationStopped.Raise(this, new StringEventArgs(content));
		}

		#endregion
	}
}
