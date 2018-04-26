using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class ConferencingServerDeviceSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "ConferencingServer";
		private const string WRAPPED_DEVICE_ID_ELEMENT = "DeviceId";
		private const string WRAPPED_CONTROL_ID_ELEMENT = "ControlId";
		private const string SERVER_PORT_ELEMENT = "ServerPort";
		private const string SERVER_CLIENTS_ELEMENT = "ServerMaxClients";

		public override string FactoryName { get { return FACTORY_NAME; } }
		public override Type OriginatorType { get { return typeof(ConferencingServerDevice); } }
		public int? WrappedDeviceId { get; set; }
		public int? WrappedControlId { get; set; }

		public ushort ServerPort { get; set; }
		public int ServerMaxClients { get; set; }

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			WrappedDeviceId = XmlUtils.TryReadChildElementContentAsInt(xml, WRAPPED_DEVICE_ID_ELEMENT);
			WrappedControlId = XmlUtils.TryReadChildElementContentAsInt(xml, WRAPPED_CONTROL_ID_ELEMENT);
			ServerPort = XmlUtils.TryReadChildElementContentAsUShort(xml, SERVER_PORT_ELEMENT) ?? 0;
			ServerMaxClients = XmlUtils.TryReadChildElementContentAsInt(xml, SERVER_CLIENTS_ELEMENT) ??
			                   AsyncTcpServer.MAX_NUMBER_OF_CLIENTS_SUPPORTED;
		}

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(WRAPPED_DEVICE_ID_ELEMENT, IcdXmlConvert.ToString(WrappedDeviceId));
			writer.WriteElementString(WRAPPED_CONTROL_ID_ELEMENT, IcdXmlConvert.ToString(WrappedControlId));
			writer.WriteElementString(SERVER_PORT_ELEMENT, IcdXmlConvert.ToString(ServerPort));
			writer.WriteElementString(SERVER_CLIENTS_ELEMENT, IcdXmlConvert.ToString(ServerMaxClients));
		}
	}
}
