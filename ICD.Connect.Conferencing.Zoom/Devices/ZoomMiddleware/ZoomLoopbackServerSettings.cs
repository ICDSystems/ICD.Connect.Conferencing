using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomMiddleware
{
	[KrangSettings("ZoomLoopbackServer", typeof(ZoomLoopbackServerDevice))]
	public sealed class ZoomLoopbackServerSettings : AbstractDeviceSettings, ISecureNetworkSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const string LISTEN_ADDRESS_ELEMENT = "ListenAddress";
		private const string LISTEN_PORT_ELEMENT = "ListenPort";

		private readonly SecureNetworkProperties m_NetworkProperties;

		#region Properties

		/// <summary>
		/// The port id.
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ISerialPort))]
		public int? Port { get; set; }

		/// <summary>
		/// The address for the TCP server to accept connections from.
		/// </summary>
		public string ListenAddress { get; set; }

		/// <summary>
		/// The port for the TCP server.
		/// </summary>
		public ushort ListenPort { get; set; }

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
		void INetworkProperties.ClearNetworkProperties()
		{
			m_NetworkProperties.ClearNetworkProperties();
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		public ZoomLoopbackServerSettings()
		{
			m_NetworkProperties = new SecureNetworkProperties();

			ListenAddress = "0.0.0.0";
			ListenPort = 2245;

			UpdateNetworkDefaults();
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(PORT_ELEMENT, Port == null ? null : IcdXmlConvert.ToString((int)Port));
			writer.WriteElementString(LISTEN_ADDRESS_ELEMENT, ListenAddress);
			writer.WriteElementString(LISTEN_PORT_ELEMENT, IcdXmlConvert.ToString(ListenPort));

			m_NetworkProperties.WriteElements(writer);
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);
			ListenAddress = XmlUtils.TryReadChildElementContentAsString(xml, LISTEN_ADDRESS_ELEMENT) ?? "0.0.0.0";
			ListenPort = XmlUtils.TryReadChildElementContentAsUShort(xml, LISTEN_PORT_ELEMENT) ?? 2245;

			m_NetworkProperties.ParseXml(xml);

			UpdateNetworkDefaults();
		}

		/// <summary>
		/// Sets default values for unconfigured network properties.
		/// </summary>
		private void UpdateNetworkDefaults()
		{
			m_NetworkProperties.ApplyDefaultValues("localhost", 2244, "zoom", "zoomus123");
		}
	}
}
