using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Tcp;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server.Devices.Simpl.Server
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class ConferencingServerDeviceSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "ConferencingServer";
		private const string WRAPPED_DEVICES_ELEMENT = "Devices";
		private const string WRAPPED_DEVICE_ELEMENT = "Device";
		private const string SERVER_PORT_ELEMENT = "ServerPort";
		private const string SERVER_CLIENTS_ELEMENT = "ServerMaxClients";

		public override string FactoryName { get { return FACTORY_NAME; } }
		public override Type OriginatorType { get { return typeof(ConferencingServerDevice); } }
		public List<int> WrappedDeviceIds { get; set; }

		public ushort ServerPort { get; set; }
		public int ServerMaxClients { get; set; }

		private int? ParseDeviceToInt(string s)
		{
			int id;
			bool isInt = StringUtils.TryParse(s, out id);

			return isInt ? id : (int?)null;
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			var listItems = XmlUtils.ReadListFromXml(xml, WRAPPED_DEVICES_ELEMENT, WRAPPED_DEVICE_ELEMENT, s => ParseDeviceToInt(s));
			WrappedDeviceIds = listItems.Where(item => item != null).Select(item => item.Value).ToList();
			
			ServerPort = XmlUtils.TryReadChildElementContentAsUShort(xml, SERVER_PORT_ELEMENT) ?? 0;
			ServerMaxClients = XmlUtils.TryReadChildElementContentAsInt(xml, SERVER_CLIENTS_ELEMENT) ??
			                   AsyncTcpServer.MAX_NUMBER_OF_CLIENTS_SUPPORTED;
		}

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			WriteDevices(writer);
			writer.WriteElementString(SERVER_PORT_ELEMENT, IcdXmlConvert.ToString(ServerPort));
			writer.WriteElementString(SERVER_CLIENTS_ELEMENT, IcdXmlConvert.ToString(ServerMaxClients));
		}

		/// <summary>
		/// Writes all devices to the xml with their room id as an attribute.
		/// </summary>
		/// <param name="writer"></param>
		private void WriteDevices(IcdXmlTextWriter writer)
		{
			writer.WriteStartElement(WRAPPED_DEVICES_ELEMENT);
			foreach (int device in WrappedDeviceIds)
			{
				writer.WriteStartElement(WRAPPED_DEVICE_ELEMENT);
				writer.WriteValue(device);
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
	}
}
