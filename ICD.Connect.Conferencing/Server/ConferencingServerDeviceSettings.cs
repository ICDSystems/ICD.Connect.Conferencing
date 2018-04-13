using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class ConferencingServerDeviceSettings : AbstractDeviceSettings
	{
		private const string FACTORY_NAME = "ConferencingServer";
		private const string WRAPPED_DEVICE_ID_ELEMENT = "DeviceId";
		private const string WRAPPED_CONTROL_ID_ELEMENT = "ControlId";

		public override string FactoryName { get { return FACTORY_NAME; } }
		public override Type OriginatorType { get { return typeof(ConferencingServerDevice); } }
		public int? WrappedDeviceId { get; set; }
		public int? WrappedControlId { get; set; }

		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			WrappedDeviceId = XmlUtils.TryReadChildElementContentAsInt(xml, WRAPPED_DEVICE_ID_ELEMENT);
			WrappedControlId = XmlUtils.TryReadChildElementContentAsInt(xml, WRAPPED_CONTROL_ID_ELEMENT);
		}

		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(WRAPPED_DEVICE_ID_ELEMENT, IcdXmlConvert.ToString(WrappedDeviceId));
			writer.WriteElementString(WRAPPED_CONTROL_ID_ELEMENT, IcdXmlConvert.ToString(WrappedControlId));
		}
	}
}
