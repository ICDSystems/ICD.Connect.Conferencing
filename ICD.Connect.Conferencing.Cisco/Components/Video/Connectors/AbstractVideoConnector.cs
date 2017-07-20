using System;
using ICD.Common.EventArguments;
using ICD.Common.Properties;
using ICD.Common.Services;
using ICD.Common.Services.Logging;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Xml;
using ICD.Connect.Routing.Connections;

namespace ICD.Connect.Conferencing.Cisco.Components.Video.Connectors
{
	// Ignore missing comment warning
#pragma warning disable 1591
	public enum eConnectorType
	{
		[UsedImplicitly] Unknown,
		[UsedImplicitly] Camera,
		[UsedImplicitly] Composite,
		[UsedImplicitly] Vga,
		[UsedImplicitly] Hdmi,
		[UsedImplicitly] Dvi,
		[UsedImplicitly] Usb
	}
#pragma warning restore 1591

	// Ignore missing comment warning
#pragma warning disable 1591
	public enum eSignalState
	{
		[UsedImplicitly] Unknown,
		[UsedImplicitly] Ok
	}
#pragma warning restore 1591

	/// <summary>
	/// Represents a video input connection.
	/// </summary>
	public abstract class AbstractVideoConnector
	{
		/// <summary>
		/// Raised when the Connected state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnConnectedStateChanged;

		private bool m_Connected;

		#region Properties

		/// <summary>
		/// The connection state.
		/// </summary>
		[PublicAPI]
		public bool Connected
		{
			get { return m_Connected; }
			private set
			{
				if (value == m_Connected)
					return;

				m_Connected = value;

				OnConnectedStateChanged.Raise(this, new BoolEventArgs(m_Connected));
			}
		}

		/// <summary>
		/// The id for the connector.
		/// </summary>
		public int ConnectorId { get; private set; }

		/// <summary>
		/// The source id for the connector.
		/// </summary>
		public int SourceId { get; private set; }

		/// <summary>
		/// The type of connection.
		/// </summary>
		public eConnectorType ConnectorType { get; private set; }

		/// <summary>
		/// Thes the connection type for routing.
		/// </summary>
		public eConnectionType ConnectionType
		{
			get
			{
				switch (ConnectorType)
				{
					case eConnectorType.Unknown:
						return eConnectionType.None;

					case eConnectorType.Camera:
					case eConnectorType.Vga:
						return eConnectionType.Video;

					case eConnectorType.Hdmi:
					case eConnectorType.Dvi:
						return eConnectionType.Video | eConnectionType.Audio;

					case eConnectorType.Usb:
						return eConnectionType.Usb;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		/// <summary>
		/// The signal state.
		/// </summary>
		[PublicAPI]
		public eSignalState SignalState { get; private set; }

		#endregion

		#region Methods

		/// <summary>
		/// Updates to match the xml values.
		/// </summary>
		/// <param name="xml"></param>
		public virtual void UpdateFromXml(string xml)
		{
			ConnectorId = GetConnectorId(xml);

			string connected = XmlUtils.TryReadChildElementContentAsString(xml, "Connected");
			if (connected != null)
				Connected = connected == "True";

			string signalState = XmlUtils.TryReadChildElementContentAsString(xml, "SignalState");
			eSignalState signalStateEnum;

			if (EnumUtils.TryParse(signalState, true, out signalStateEnum))
				SignalState = signalStateEnum;
			else if (!string.IsNullOrEmpty(signalState))
			{
				ServiceProvider.GetService<ILoggerService>()
				               .AddEntry(eSeverity.Warning, "{0} - Unknown signal state {1}", GetType().Name, signalState);
				SignalState = eSignalState.Unknown;
			}

			string sourceId = XmlUtils.TryReadChildElementContentAsString(xml, "SourceId");
			if (sourceId != null)
				SourceId = int.Parse(sourceId);

			string type = XmlUtils.TryReadChildElementContentAsString(xml, "Type");
			eConnectorType connectorType;

			if (EnumUtils.TryParse(type, true, out connectorType))
				ConnectorType = connectorType;
			else if (!string.IsNullOrEmpty(type))
			{
				ServiceProvider.GetService<ILoggerService>()
							   .AddEntry(eSeverity.Warning, "{0} - Unknown connector type {1}", GetType().Name, connectorType);
				ConnectorType = eConnectorType.Unknown;
			}
		}

		/// <summary>
		/// Gets the connector id from xml.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static int GetConnectorId(string xml)
		{
			return XmlUtils.GetAttributeAsInt(xml, "item");
		}

		#endregion
	}
}
