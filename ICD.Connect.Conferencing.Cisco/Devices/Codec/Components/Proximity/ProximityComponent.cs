using System;
using ICD.Common.Logging.LoggingContexts;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Proximity
{
	/// <summary>
	/// ProximityComponent provides functionality for controlling proximity sharing features.
	/// </summary>
	public sealed class ProximityComponent : AbstractCiscoComponent
	{
		#region Events

		/// <summary>
		/// Raised when the proximity mode changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<ProximityModeEventArgs> OnProximityModeChanged;

		/// <summary>
		/// Raised when the proximity call control state changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<ProximityServicesEventArgs> OnProximityCallControlChanged;

		/// <summary>
		/// Raised when the proximity content share from clients state changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<ProximityServicesEventArgs> OnProximityContentShareFromClientsChanged;

		/// <summary>
		/// Raised when the proximity content share to clients state changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<ProximityServicesEventArgs> OnProximityContentShareToClientsChanged;

		#endregion

		#region Fields

		private eProximityMode m_ProximityMode;
		private eProximityServiceState m_CallControlServiceState;
		private eProximityServiceState m_ContentShareFromClientsState;
		private eProximityServiceState m_ContentShareToClientsState;

		#endregion

		#region Properties

		/// <summary>
		/// Gets the current proximity mode.
		/// </summary>
		[PublicAPI]
		public eProximityMode ProximityMode
		{
			get { return m_ProximityMode; }
			private set
			{
				if (value == m_ProximityMode)
					return;

				m_ProximityMode = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "ProximityMode", m_ProximityMode);

				OnProximityModeChanged.Raise(this, new ProximityModeEventArgs(m_ProximityMode));
			}
		}

		/// <summary>
		/// Gets the current enabled state for the proximity call control service.
		/// </summary>
		[PublicAPI]
		public eProximityServiceState CallControlServiceState
		{
			get { return m_CallControlServiceState; }
			private set
			{
				if (value == m_CallControlServiceState)
					return;

				m_CallControlServiceState = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "CallControlServiceState", m_CallControlServiceState);

				OnProximityCallControlChanged.Raise(this, new ProximityServicesEventArgs(m_CallControlServiceState));
			}
		}

		/// <summary>
		/// Gets the current enabled state for the proximity content share from clients service.
		/// </summary>
		[PublicAPI]
		public eProximityServiceState ContentShareFromClientsServiceState
		{
			get { return m_ContentShareFromClientsState; }
			private set
			{
				if (value == m_ContentShareFromClientsState)
					return;

				m_ContentShareFromClientsState = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "ContentShareFromClientsState", m_ContentShareFromClientsState);

				OnProximityContentShareFromClientsChanged.Raise(this, new ProximityServicesEventArgs(m_ContentShareFromClientsState));
			}
		}

		/// <summary>
		/// Gets the current enabled state for the proximity content share to clients service.
		/// </summary>
		[PublicAPI]
		public eProximityServiceState ContentShareToClientsServiceState
		{
			get { return m_ContentShareToClientsState; }
			private set
			{
				if (value == m_ContentShareToClientsState)
					return;

				m_ContentShareToClientsState = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "ContentShareToClientsState", m_ContentShareToClientsState);

				OnProximityContentShareToClientsChanged.Raise(this, new ProximityServicesEventArgs(m_ContentShareToClientsState));
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public ProximityComponent(CiscoCodecDevice codec)
			: base(codec)
		{
			Subscribe(codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			OnProximityModeChanged = null;
			OnProximityCallControlChanged = null;
			OnProximityContentShareFromClientsChanged = null;
			OnProximityContentShareToClientsChanged = null;

			base.Dispose(disposing);
		}

		protected override void Initialize()
		{
			base.Initialize();

			Codec.SendCommand("xConfiguration Proximity Mode");
			Codec.SendCommand("xConfiguration Proximity Services CallControl");
			Codec.SendCommand("xConfiguration Proximity Services ContentShare FromClients");
			Codec.SendCommand("xConfiguration Proximity Services ContentShare ToClients");
		}

		/// <summary>
		/// Sets the proximity mode.
		/// </summary>
		/// <param name="mode"></param>
		[PublicAPI]
		public void SetProximityMode(eProximityMode mode)
		{
			Codec.SendCommand("xConfiguration Proximity Mode: {0}", mode);
			Codec.Logger.Log(eSeverity.Informational, "Setting Proximity Mode to: {0}", mode);
		}

		/// <summary>
		/// Sets the Proximity Call Control Service state.
		/// </summary>
		/// <param name="state"></param>
		[PublicAPI]
		public void SetProximityCallControl(eProximityServiceState state)
		{
			Codec.SendCommand("xConfiguration Proximity Services CallControl: {0}", state);
			Codec.Logger.Log(eSeverity.Informational, "Setting Proximity Call Control Service state to: {0}", state);
		}

		/// <summary>
		/// Sets the Proximity Content Sharing From Clients Service state.
		/// </summary>
		/// <param name="state"></param>
		[PublicAPI]
		public void SetProximityContentSharingFromClients(eProximityServiceState state)
		{
			Codec.SendCommand("xConfiguration Proximity Services ContentShare FromClients: {0}", state);
			Codec.Logger.Log(eSeverity.Informational, "Setting Proximity Content Sharing From Clients Service state to: {0}", state);
		}

		/// <summary>
		/// Sets the Proximity Content Sharing To Clients Service state.
		/// </summary>
		/// <param name="state"></param>
		[PublicAPI]
		public void SetProximityContentSharingToClients(eProximityServiceState state)
		{
			Codec.SendCommand("xConfiguration Proximity Services ContentShare ToClients: {0}", state);
			Codec.Logger.Log(eSeverity.Informational, "Setting Proximity Content Sharing To Clients Service state to: {0}", state);
		}

		#endregion

		#region Codec Callbacks

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseProximityMode, CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Proximity",
			                             "Mode");
			codec.RegisterParserCallback(ParseProximityServicesCallControl, CiscoCodecDevice.XCONFIGURATION_ELEMENT,
			                             "Proximity", "Services", "CallControl");
			codec.RegisterParserCallback(ParseProximityServicesContentShareFromClients,
			                             CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Proximity", "Services",
			                             "ContentShare", "FromClients");
			codec.RegisterParserCallback(ParseProximityServicesContentShareToClients,
			                             CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Proximity", "Services",
			                             "ContentShare", "ToClients");
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

			codec.UnregisterParserCallback(ParseProximityMode, CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Proximity",
			                             "Mode");
			codec.UnregisterParserCallback(ParseProximityServicesCallControl, CiscoCodecDevice.XCONFIGURATION_ELEMENT,
			                               "Proximity", "Services", "CallControl");
			codec.UnregisterParserCallback(ParseProximityServicesContentShareFromClients,
			                               CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Proximity", "Services",
			                               "ContentShare", "FromClients");
			codec.UnregisterParserCallback(ParseProximityServicesContentShareToClients,
			                               CiscoCodecDevice.XCONFIGURATION_ELEMENT, "Proximity", "Services",
			                               "ContentShare", "ToClients");
		}

		private void ParseProximityMode(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			ProximityMode = EnumUtils.Parse<eProximityMode>(content, true);
		}

		private void ParseProximityServicesCallControl(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			CallControlServiceState = EnumUtils.Parse<eProximityServiceState>(content, true);
		}

		private void ParseProximityServicesContentShareFromClients(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			ContentShareFromClientsServiceState = EnumUtils.Parse<eProximityServiceState>(content, true);
		}

		private void ParseProximityServicesContentShareToClients(CiscoCodecDevice codec, string resultid, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			ContentShareToClientsServiceState = EnumUtils.Parse<eProximityServiceState>(content, true);
		}

		#endregion
	}
}
