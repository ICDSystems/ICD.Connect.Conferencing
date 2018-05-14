using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using ICD.Connect.Protocol.Settings;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Cisco
{
	/// <summary>
	/// Settings for the CiscoCodec.
	/// </summary>
	[KrangSettings("CiscoCodec", typeof(CiscoCodec))]
	public sealed class CiscoCodecSettings : AbstractDeviceSettings, INetworkProperties, IComSpecProperties
	{
		private const string PORT_ELEMENT = "Port";
		private const string PERIPHERALS_ID_ELEMENT = "PeripheralsID";

		private const string INPUT_1_ELEMENT = "Input1Type";
		private const string INPUT_2_ELEMENT = "Input2Type";
		private const string INPUT_3_ELEMENT = "Input3Type";
		private const string INPUT_4_ELEMENT = "Input4Type";

		private readonly NetworkProperties m_NetworkProperties;
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

		#endregion

		#region Inputs

		public eCodecInputType Input1CodecInputType { get; set; }
		public eCodecInputType Input2CodecInputType { get; set; }
		public eCodecInputType Input3CodecInputType { get; set; }
		public eCodecInputType Input4CodecInputType { get; set; }

		#endregion

		#region Network

		/// <summary>
		/// Gets/sets the configurable username.
		/// </summary>
		public string Username { get { return m_NetworkProperties.Username; } set { m_NetworkProperties.Username = value; } }

		/// <summary>
		/// Gets/sets the configurable password.
		/// </summary>
		public string Password { get { return m_NetworkProperties.Password; } set { m_NetworkProperties.Password = value; } }

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
		public ushort NetworkPort
		{
			get { return m_NetworkProperties.NetworkPort; }
			set { m_NetworkProperties.NetworkPort = value; }
		}

		#endregion

		#region Com Spec

		/// <summary>
		/// Gets/sets the configurable baud rate.
		/// </summary>
		public eComBaudRates ComSpecBaudRate
		{
			get { return m_ComSpecProperties.ComSpecBaudRate; }
			set { m_ComSpecProperties.ComSpecBaudRate = value; }
		}

		/// <summary>
		/// Gets/sets the configurable number of data bits.
		/// </summary>
		public eComDataBits ComSpecNumberOfDataBits
		{
			get { return m_ComSpecProperties.ComSpecNumberOfDataBits; }
			set { m_ComSpecProperties.ComSpecNumberOfDataBits = value; }
		}

		/// <summary>
		/// Gets/sets the configurable parity type.
		/// </summary>
		public eComParityType ComSpecParityType
		{
			get { return m_ComSpecProperties.ComSpecParityType; }
			set { m_ComSpecProperties.ComSpecParityType = value; }
		}

		/// <summary>
		/// Gets/sets the configurable number of stop bits.
		/// </summary>
		public eComStopBits ComSpecNumberOfStopBits
		{
			get { return m_ComSpecProperties.ComSpecNumberOfStopBits; }
			set { m_ComSpecProperties.ComSpecNumberOfStopBits = value; }
		}

		/// <summary>
		/// Gets/sets the configurable protocol type.
		/// </summary>
		public eComProtocolType ComSpecProtocolType
		{
			get { return m_ComSpecProperties.ComSpecProtocolType; }
			set { m_ComSpecProperties.ComSpecProtocolType = value; }
		}

		/// <summary>
		/// Gets/sets the configurable hardware handshake type.
		/// </summary>
		public eComHardwareHandshakeType ComSpecHardwareHandShake
		{
			get { return m_ComSpecProperties.ComSpecHardwareHandShake; }
			set { m_ComSpecProperties.ComSpecHardwareHandShake = value; }
		}

		/// <summary>
		/// Gets/sets the configurable software handshake type.
		/// </summary>
		public eComSoftwareHandshakeType ComSpecSoftwareHandshake
		{
			get { return m_ComSpecProperties.ComSpecSoftwareHandshake; }
			set { m_ComSpecProperties.ComSpecSoftwareHandshake = value; }
		}

		/// <summary>
		/// Gets/sets the configurable report CTS changes state.
		/// </summary>
		public bool ComSpecReportCtsChanges
		{
			get { return m_ComSpecProperties.ComSpecReportCtsChanges; }
			set { m_ComSpecProperties.ComSpecReportCtsChanges = value; }
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public CiscoCodecSettings()
		{
			PeripheralsId = Guid.NewGuid().ToString();

			m_NetworkProperties = new NetworkProperties();
			m_ComSpecProperties = new ComSpecProperties();
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

			writer.WriteElementString(INPUT_1_ELEMENT, IcdXmlConvert.ToString(Input1CodecInputType));
			writer.WriteElementString(INPUT_2_ELEMENT, IcdXmlConvert.ToString(Input2CodecInputType));
			writer.WriteElementString(INPUT_3_ELEMENT, IcdXmlConvert.ToString(Input3CodecInputType));
			writer.WriteElementString(INPUT_4_ELEMENT, IcdXmlConvert.ToString(Input4CodecInputType));

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

			Input1CodecInputType = XmlUtils.TryReadChildElementContentAsEnum<eCodecInputType>(xml, INPUT_1_ELEMENT, true) ?? eCodecInputType.None;
			Input2CodecInputType = XmlUtils.TryReadChildElementContentAsEnum<eCodecInputType>(xml, INPUT_2_ELEMENT, true) ?? eCodecInputType.None;
			Input3CodecInputType = XmlUtils.TryReadChildElementContentAsEnum<eCodecInputType>(xml, INPUT_3_ELEMENT, true) ?? eCodecInputType.None;
			Input4CodecInputType = XmlUtils.TryReadChildElementContentAsEnum<eCodecInputType>(xml, INPUT_4_ELEMENT, true) ?? eCodecInputType.None;

			m_NetworkProperties.ParseXml(xml);
			m_ComSpecProperties.ParseXml(xml);
		}
	}
}
