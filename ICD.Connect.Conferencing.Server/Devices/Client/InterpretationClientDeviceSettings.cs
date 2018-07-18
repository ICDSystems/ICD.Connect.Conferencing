using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class InterpretationClientDeviceSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "InterpretationClient";

		private const string PORT_ELEMENT = "Port";
		private const string ROOM_ID_ELEMENT = "Room";
		private const string ROOM_NAME_ELEMENT = "RoomName";
		private const string ROOM_PREFIX_ELEMENT = "RoomPrefix";

		[PublicAPI, OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		[PublicAPI]
		public int? Room { get; set; }

		[PublicAPI]
		public string RoomName { get; set; }

		[PublicAPI]
		public string RoomPrefix { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ROOM_ID_ELEMENT, IcdXmlConvert.ToString(Room));
			writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString(Port));
			writer.WriteElementString(ROOM_NAME_ELEMENT, IcdXmlConvert.ToString(RoomName));
			writer.WriteElementString(ROOM_PREFIX_ELEMENT, IcdXmlConvert.ToString(RoomPrefix));
		}

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(InterpretationClientDevice); } }

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Room = XmlUtils.TryReadChildElementContentAsInt(xml, ROOM_ID_ELEMENT);
			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
			RoomName = XmlUtils.TryReadChildElementContentAsString(xml, ROOM_NAME_ELEMENT);
			RoomPrefix = XmlUtils.TryReadChildElementContentAsString(xml, ROOM_PREFIX_ELEMENT);
		}
	}
}
