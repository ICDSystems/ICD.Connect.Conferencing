using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec
{
	/// <summary>
	/// Settings for the CiscoCodec.
	/// </summary>
	[KrangSettings("CiscoCodec", typeof(CiscoCodecDevice))]
	public sealed class CiscoCodecSettings : AbstractVideoConferenceDeviceSettings, ISecureNetworkSettings, IComSpecSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const string PERIPHERALS_ID_ELEMENT = "PeripheralsID";
		private const string PHONEBOOK_TYPE_ELEMENT = "PhonebookType";
		private const string PRESENTER_TRACK_CAMERA_ELEMENT = "PresenterTrackCameraID";
		private const string STANDBY_TO_HALFWAKE_ELEMENT = "StandbyToHalfwake";

		private readonly SecureNetworkProperties m_NetworkProperties;
		private readonly ComSpecProperties m_ComSpecProperties;

		private string m_PeripheralsId;

		#region Properties

		/// <summary>
		/// The port id.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		/// <summary>
		/// Gets/sets the peripherals id.
		/// </summary>
		public string PeripheralsId
		{
			get
			{
				if (string.IsNullOrEmpty(m_PeripheralsId))
					m_PeripheralsId = Guid.NewGuid().ToString();
				return m_PeripheralsId;
			}
			set { m_PeripheralsId = value; }
		}

		/// <summary>
		/// Determines which phonebook to use with directory.
		/// </summary>
		public ePhonebookType PhonebookType { get; set; }

		/// <summary>
		/// If true, the system will go into halfwake mode instead of sleep mode when standby/poweroff is run
		/// </summary>
		/// <remarks>This is a workaround for a compatibility issue with Room Kit Plus and DMPS3 rooms</remarks>
		public bool StandbyToHalfwake { get; set; }

		#endregion

		#region Network

		/// <summary>
		/// Gets/sets the configurable network username.
		/// </summary>
		public string NetworkUsername
		{
			get { return m_NetworkProperties.NetworkUsername; }
			set { m_NetworkProperties.NetworkUsername = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network password.
		/// </summary>
		public string NetworkPassword
		{
			get { return m_NetworkProperties.NetworkPassword; }
			set { m_NetworkProperties.NetworkPassword = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress
		{
			get { return m_NetworkProperties.NetworkAddress; }
			set { m_NetworkProperties.NetworkAddress = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort? NetworkPort
		{
			get { return m_NetworkProperties.NetworkPort; }
			set { m_NetworkProperties.NetworkPort = value; }
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void INetworkProperties.ClearNetworkProperties()
		{
			m_NetworkProperties.ClearNetworkProperties();
		}

		#endregion

		#region Com Spec

		/// <summary>
		/// Gets/sets the configurable baud rate.
		/// </summary>
		public eComBaudRates? ComSpecBaudRate
		{
			get { return m_ComSpecProperties.ComSpecBaudRate; }
			set { m_ComSpecProperties.ComSpecBaudRate = value; }
		}

		/// <summary>
		/// Gets/sets the configurable number of data bits.
		/// </summary>
		public eComDataBits? ComSpecNumberOfDataBits
		{
			get { return m_ComSpecProperties.ComSpecNumberOfDataBits; }
			set { m_ComSpecProperties.ComSpecNumberOfDataBits = value; }
		}

		/// <summary>
		/// Gets/sets the configurable parity type.
		/// </summary>
		public eComParityType? ComSpecParityType
		{
			get { return m_ComSpecProperties.ComSpecParityType; }
			set { m_ComSpecProperties.ComSpecParityType = value; }
		}

		/// <summary>
		/// Gets/sets the configurable number of stop bits.
		/// </summary>
		public eComStopBits? ComSpecNumberOfStopBits
		{
			get { return m_ComSpecProperties.ComSpecNumberOfStopBits; }
			set { m_ComSpecProperties.ComSpecNumberOfStopBits = value; }
		}

		/// <summary>
		/// Gets/sets the configurable protocol type.
		/// </summary>
		public eComProtocolType? ComSpecProtocolType
		{
			get { return m_ComSpecProperties.ComSpecProtocolType; }
			set { m_ComSpecProperties.ComSpecProtocolType = value; }
		}

		/// <summary>
		/// Gets/sets the configurable hardware handshake type.
		/// </summary>
		public eComHardwareHandshakeType? ComSpecHardwareHandshake
		{
			get { return m_ComSpecProperties.ComSpecHardwareHandshake; }
			set { m_ComSpecProperties.ComSpecHardwareHandshake = value; }
		}

		/// <summary>
		/// Gets/sets the configurable software handshake type.
		/// </summary>
		public eComSoftwareHandshakeType? ComSpecSoftwareHandshake
		{
			get { return m_ComSpecProperties.ComSpecSoftwareHandshake; }
			set { m_ComSpecProperties.ComSpecSoftwareHandshake = value; }
		}

		/// <summary>
		/// Gets/sets the configurable report CTS changes state.
		/// </summary>
		public bool? ComSpecReportCtsChanges
		{
			get { return m_ComSpecProperties.ComSpecReportCtsChanges; }
			set { m_ComSpecProperties.ComSpecReportCtsChanges = value; }
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void IComSpecProperties.ClearComSpecProperties()
		{
			m_ComSpecProperties.ClearComSpecProperties();
		}

		#endregion

		/// <summary>
		/// Determines which camera to use with PresenterTrack features.
		/// </summary>
		public int? PresenterTrackCameraId { get; set; }

		/// <summary>
		/// Constructor.
		/// </summary>
		public CiscoCodecSettings()
		{
			PeripheralsId = Guid.NewGuid().ToString();

			m_NetworkProperties = new SecureNetworkProperties();
			m_ComSpecProperties = new ComSpecProperties();

			UpdateNetworkDefaults();
			UpdateComSpecDefaults();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, Port == null ? null : IcdXmlConvert.ToString((int)Port));
			writer.WriteElementString(PERIPHERALS_ID_ELEMENT, PeripheralsId);
			writer.WriteElementString(PHONEBOOK_TYPE_ELEMENT, IcdXmlConvert.ToString(PhonebookType));
			writer.WriteElementString(PRESENTER_TRACK_CAMERA_ELEMENT, IcdXmlConvert.ToString(PresenterTrackCameraId));
			writer.WriteElementString(STANDBY_TO_HALFWAKE_ELEMENT, IcdXmlConvert.ToString(StandbyToHalfwake));

			m_NetworkProperties.WriteElements(writer);
			m_ComSpecProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
			PeripheralsId = XmlUtils.TryReadChildElementContentAsString(xml, PERIPHERALS_ID_ELEMENT);
			PhonebookType = XmlUtils.TryReadChildElementContentAsEnum<ePhonebookType>(xml, PHONEBOOK_TYPE_ELEMENT, true) ??
			                ePhonebookType.Corporate;
			PresenterTrackCameraId = XmlUtils.TryReadChildElementContentAsInt(xml, PRESENTER_TRACK_CAMERA_ELEMENT);
			StandbyToHalfwake = XmlUtils.TryReadChildElementContentAsBoolean(xml, STANDBY_TO_HALFWAKE_ELEMENT) ?? false;

			m_NetworkProperties.ParseXml(xml);
			m_ComSpecProperties.ParseXml(xml);

			UpdateNetworkDefaults();
			UpdateComSpecDefaults();
		}

		/// <summary>
		/// Sets default values for unconfigured network properties.
		/// </summary>
		private void UpdateNetworkDefaults()
		{
			m_NetworkProperties.ApplyDefaultValues(null, 22);
		}

		/// <summary>
		/// Sets default values for unconfigured comspec properties.
		/// </summary>
		private void UpdateComSpecDefaults()
		{
			m_ComSpecProperties.ApplyDefaultValues(eComBaudRates.BaudRate115200,
												   eComDataBits.DataBits8,
												   eComParityType.None,
												   eComStopBits.StopBits1,
												   eComProtocolType.Rs232,
												   eComHardwareHandshakeType.None,
												   eComSoftwareHandshakeType.None,
												   false);
		}
	}
}
