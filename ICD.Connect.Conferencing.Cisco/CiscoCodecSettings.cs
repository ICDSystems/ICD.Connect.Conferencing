using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Cisco
{
	/// <summary>
	/// Settings for the CiscoCodec.
	/// </summary>
	[KrangSettings(FACTORY_NAME)]
	public sealed class CiscoCodecSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "CiscoCodec";

		private const string PORT_ELEMENT = "Port";
		private const string PERIPHERALS_ID_ELEMENT = "PeripheralsID";

		private const string INPUT_1_ELEMENT = "Input1Type";
		private const string INPUT_2_ELEMENT = "Input2Type";
		private const string INPUT_3_ELEMENT = "Input3Type";
		private const string INPUT_4_ELEMENT = "Input4Type";

		private string m_PeripheralsId;

		/// <summary>
		/// The port id.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		public eCodecInputType Input1CodecInputType { get; set; }
		public eCodecInputType Input2CodecInputType { get; set; }
		public eCodecInputType Input3CodecInputType { get; set; }
		public eCodecInputType Input4CodecInputType { get; set; }

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
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(CiscoCodec); } }

		/// <summary>
		/// Constructor.
		/// </summary>
		public CiscoCodecSettings()
		{
			PeripheralsId = Guid.NewGuid().ToString();
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

			writer.WriteElementString(INPUT_1_ELEMENT, Input1CodecInputType.ToString());
			writer.WriteElementString(INPUT_2_ELEMENT, Input2CodecInputType.ToString());
			writer.WriteElementString(INPUT_3_ELEMENT, Input3CodecInputType.ToString());
			writer.WriteElementString(INPUT_4_ELEMENT, Input4CodecInputType.ToString());
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
		}
	}
}
