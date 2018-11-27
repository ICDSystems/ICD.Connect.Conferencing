using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Zoom
{
	[KrangSettings("ZoomRoom", typeof(ZoomRoom))]
	public sealed class ZoomRoomSettings : AbstractVideoConferenceDeviceSettings
	{
		private const string PORT_ELEMENT = "Port";

		/// <summary>
		/// The port id.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		private const string INPUT_1_ELEMENT = "Input1Id";
		private const string INPUT_2_ELEMENT = "Input2Id";
		private const string INPUT_3_ELEMENT = "Input3Id";
		private const string INPUT_4_ELEMENT = "Input4Id";

		public string Input1CodecInputId { get; set; }
		public string Input2CodecInputId { get; set; }
		public string Input3CodecInputId { get; set; }
		public string Input4CodecInputId { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, Port == null ? null : IcdXmlConvert.ToString((int) Port));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
		}
	}
}