using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Addressbook;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec
{
	[KrangSettings("PolycomGroupSeries", typeof(PolycomGroupSeriesDevice))]
	public sealed class PolycomGroupSeriesSettings : AbstractVideoConferenceDeviceSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const string PASSWORD_ELEMENT = "Password";
		private const string ADDRESSBOOK_TYPE_ELEMENT = "AddressbookType";

		private const string DEFAULT_PASSWORD = "admin";

		private string m_Password;

		/// <summary>
		/// The port id.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		public string Password
		{
			get
			{
				if (string.IsNullOrEmpty(m_Password))
					m_Password = DEFAULT_PASSWORD;
				return m_Password;
			}
			set { m_Password = value; }
		}

		/// <summary>
		/// Determines which addressbook to use with directory.
		/// </summary>
		public eAddressbookType AddressbookType { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, Port == null ? null : IcdXmlConvert.ToString((int)Port));
			writer.WriteElementString(PASSWORD_ELEMENT, Password);
			writer.WriteElementString(ADDRESSBOOK_TYPE_ELEMENT, IcdXmlConvert.ToString(AddressbookType));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
			Password = XmlUtils.TryReadChildElementContentAsString(xml, PASSWORD_ELEMENT);
			AddressbookType = XmlUtils.TryReadChildElementContentAsEnum<eAddressbookType>(xml, ADDRESSBOOK_TYPE_ELEMENT, true) ??
			                  eAddressbookType.Global;
		}
	}
}
