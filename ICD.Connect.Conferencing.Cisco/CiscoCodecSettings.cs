using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Cisco
{
	/// <summary>
	/// Settings for the CiscoCodec.
	/// </summary>
	[KrangSettings("CiscoCodec", typeof(CiscoCodec))]
	public sealed class CiscoCodecSettings : AbstractConferencingDeviceSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const string PERIPHERALS_ID_ELEMENT = "PeripheralsID";

		private string m_PeripheralsId;

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
		}
	}
}
