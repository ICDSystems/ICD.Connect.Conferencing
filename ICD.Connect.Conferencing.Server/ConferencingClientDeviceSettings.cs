using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class ConferencingClientDeviceSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "ConferencingClient";
		private const string PORT_ELEMENT = "Port";

		public override string FactoryName { get { return FACTORY_NAME; } }

		public override Type OriginatorType { get { return typeof(ConferencingClientDevice); } }

		[PublicAPI]
		public int? Port { get; set; }

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString(Port));
		}

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
		}
	}
}
