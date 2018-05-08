using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class ConferencingClientDeviceSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "ConferencingClient";
		private const string PORT_ELEMENT = "Port";
		private const string ROOM_ID_ELEMENT = "Room";

		public override string FactoryName { get { return FACTORY_NAME; } }

		public override Type OriginatorType { get { return typeof(ConferencingClientDevice); } }

		[PublicAPI]
		public int? Port { get; set; }

		[PublicAPI]
		public int? Room { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ROOM_ID_ELEMENT, IcdXmlConvert.ToString(Room));
			writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString(Port));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Room = XmlUtils.TryReadChildElementContentAsInt(xml, ROOM_ID_ELEMENT);
			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
		}
	}
}
