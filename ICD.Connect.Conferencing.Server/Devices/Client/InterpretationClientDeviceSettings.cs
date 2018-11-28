using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Server.Devices.Client
{
	[KrangSettings("InterpretationClient", typeof(InterpretationClientDevice))]
	public sealed class InterpretationClientDeviceSettings : AbstractDeviceSettings, ISecureNetworkSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const string ROOM_ID_ELEMENT = "Room";
		private const string ROOM_NAME_ELEMENT = "RoomName";
		private const string ROOM_PREFIX_ELEMENT = "RoomPrefix";

		private readonly SecureNetworkProperties m_NetworkProperties;

		#region Properties

		[PublicAPI, OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		[PublicAPI]
		public int? Room { get; set; }

		[PublicAPI]
		public string RoomName { get; set; }

		[PublicAPI]
		public string RoomPrefix { get; set; }

		#endregion

		#region Network

		/// <summary>
		/// Gets/sets the configurable network username.
		/// </summary>
		public string NetworkUsername
		{
			get { return m_NetworkProperties.NetworkUsername; }
			set { m_NetworkProperties.NetworkUsername = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network password.
		/// </summary>
		public string NetworkPassword
		{
			get { return m_NetworkProperties.NetworkPassword; }
			set { m_NetworkProperties.NetworkPassword = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network address.
		/// </summary>
		public string NetworkAddress
		{
			get { return m_NetworkProperties.NetworkAddress; }
			set { m_NetworkProperties.NetworkAddress = value; }
		}

		/// <summary>
		/// Gets/sets the configurable network port.
		/// </summary>
		public ushort? NetworkPort
		{
			get { return m_NetworkProperties.NetworkPort; }
			set { m_NetworkProperties.NetworkPort = value; }
		}

		/// <summary>
		/// Clears the configured values.
		/// </summary>
		void INetworkProperties.Clear()
		{
			m_NetworkProperties.Clear();
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public InterpretationClientDeviceSettings()
		{
			m_NetworkProperties = new SecureNetworkProperties();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(ROOM_ID_ELEMENT, IcdXmlConvert.ToString(Room));
			writer.WriteElementString(PORT_ELEMENT, IcdXmlConvert.ToString(Port));
			writer.WriteElementString(ROOM_NAME_ELEMENT, IcdXmlConvert.ToString(RoomName));
			writer.WriteElementString(ROOM_PREFIX_ELEMENT, IcdXmlConvert.ToString(RoomPrefix));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
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
