using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
	[KrangSettings("InterpretationClient", typeof(InterpretationClientDevice))]
	public sealed class InterpretationClientDeviceSettings : AbstractDeviceSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const string ROOM_ID_ELEMENT = "Room";

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
