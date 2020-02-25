﻿using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Cameras.Devices;
using ICD.Connect.Conferencing.Devices;
using ICD.Connect.Protocol.Network.Settings;
using ICD.Connect.Protocol.Ports;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Zoom
{
	[KrangSettings("ZoomRoom", typeof(ZoomRoom))]
	public sealed class ZoomRoomSettings : AbstractVideoConferenceDeviceSettings, ISecureNetworkSettings
	{
		private const string PORT_ELEMENT = "Port";
		private const string DIAL_OUT_ENABLED_ELEMENT = "DialOutEnabled";
		private const string RECORD_ENABLED_ELEMENT = "RecordEnabled";
		private const string MUTE_MY_CAMERA_ON_START_ELEMENT = "MuteMyCameraOnStart";
		private const string MUTE_PARTICIPANTS_ON_START_ELEMENT = "MuteParticipantsOnStart";

		public const bool DEFAULT_DIAL_OUT_ENABLED = true;
		public const bool DEFAULT_RECORD_ENABLED = true;

		private const string DEFAULT_CAMERA_USB_ELEMENT = "DefaultCameraUsb";
		private const string CAMERA_1_ELEMENT = "Camera1";
		private const string CAMERA_1_USB_ELEMENT = "Camera1Usb";
		private const string CAMERA_2_ELEMENT = "Camera2";
		private const string CAMERA_2_USB_ELEMENT = "Camera2Usb";
		private const string CAMERA_3_ELEMENT = "Camera3";
		private const string CAMERA_3_USB_ELEMENT = "Camera3Usb";
		private const string CAMERA_4_ELEMENT = "Camera4";
		private const string CAMERA_4_USB_ELEMENT = "Camera4Usb";

		private readonly SecureNetworkProperties m_NetworkProperties;

		#region Properties

        /// <summary>
        /// The port id.
        /// </summary>
        [OriginatorIdSettingsProperty(typeof(ISerialPort))]
        public int? Port { get; set; }

        public bool DialOutEnabled { get; set; }

        public bool RecordEnabled { get; set; }

        public bool MuteMyCameraOnStart { get; set; }

        public bool MuteParticipantsOnStart { get; set; }

		/// <summary>
		/// Camera1 USB ID
		/// </summary>
		[CanBeNull]
		public string DefaultCameraUsb { get; set; }

		/// <summary>
		/// Camera1 Originator ID
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ICameraDevice))]
		public int? Camera1 { get; set; }

		/// <summary>
		/// Camera1 USB ID
		/// </summary>
		[CanBeNull]
		public string Camera1Usb { get; set; }

		/// <summary>
		/// Camera2 Originator ID
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ICameraDevice))]
		public int? Camera2 { get; set; }

		/// <summary>
		/// Camera2 USB ID
		/// </summary>
		[CanBeNull]
		public string Camera2Usb { get; set; }

		/// <summary>
		/// Camera3 Originator ID
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ICameraDevice))]
		public int? Camera3 { get; set; }

		/// <summary>
		/// Camera3 USB ID
		/// </summary>
		[CanBeNull]
		public string Camera3Usb { get; set; }

		/// <summary>
		/// Camera4 Originator ID
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(ICameraDevice))]
		public int? Camera4 { get; set; }

		/// <summary>
		/// Camera4 USB ID
		/// </summary>
		[CanBeNull]
        public string Camera4Usb { get; set; }

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
		public ZoomRoomSettings()
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

			writer.WriteElementString(PORT_ELEMENT, Port == null ? null : IcdXmlConvert.ToString((int) Port));

			writer.WriteElementString(DEFAULT_CAMERA_USB_ELEMENT, DefaultCameraUsb);
			writer.WriteElementString(CAMERA_1_ELEMENT, Camera1 == null ? null : IcdXmlConvert.ToString((int) Camera1));
			writer.WriteElementString(CAMERA_1_USB_ELEMENT, Camera1Usb);
			writer.WriteElementString(CAMERA_2_ELEMENT, Camera2 == null ? null : IcdXmlConvert.ToString((int) Camera2));
			writer.WriteElementString(CAMERA_2_USB_ELEMENT, Camera2Usb);
			writer.WriteElementString(CAMERA_3_ELEMENT, Camera3 == null ? null : IcdXmlConvert.ToString((int) Camera3));
			writer.WriteElementString(CAMERA_3_USB_ELEMENT, Camera3Usb);
			writer.WriteElementString(CAMERA_4_ELEMENT, Camera4 == null ? null : IcdXmlConvert.ToString((int) Camera4));
			writer.WriteElementString(CAMERA_4_USB_ELEMENT, Camera4Usb);

			m_NetworkProperties.WriteElements(writer);

			writer.WriteElementString(DIAL_OUT_ENABLED_ELEMENT, IcdXmlConvert.ToString(DialOutEnabled));
			writer.WriteElementString(RECORD_ENABLED_ELEMENT, IcdXmlConvert.ToString(RecordEnabled));
			writer.WriteElementString(MUTE_MY_CAMERA_ON_START_ELEMENT, IcdXmlConvert.ToString(MuteMyCameraOnStart));
			writer.WriteElementString(MUTE_PARTICIPANTS_ON_START_ELEMENT, IcdXmlConvert.ToString(MuteParticipantsOnStart));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Port = XmlUtils.TryReadChildElementContentAsInt(xml, PORT_ELEMENT);

			DefaultCameraUsb = XmlUtils.TryReadChildElementContentAsString(xml, DEFAULT_CAMERA_USB_ELEMENT);
			Camera1 = XmlUtils.TryReadChildElementContentAsInt(xml, CAMERA_1_ELEMENT);
			Camera1Usb = XmlUtils.TryReadChildElementContentAsString(xml, CAMERA_1_USB_ELEMENT);
			Camera2 = XmlUtils.TryReadChildElementContentAsInt(xml, CAMERA_2_ELEMENT);
			Camera2Usb = XmlUtils.TryReadChildElementContentAsString(xml, CAMERA_2_USB_ELEMENT);
			Camera3 = XmlUtils.TryReadChildElementContentAsInt(xml, CAMERA_3_ELEMENT);
			Camera3Usb = XmlUtils.TryReadChildElementContentAsString(xml, CAMERA_3_USB_ELEMENT);
			Camera4 = XmlUtils.TryReadChildElementContentAsInt(xml, CAMERA_4_ELEMENT);
			Camera4Usb = XmlUtils.TryReadChildElementContentAsString(xml, CAMERA_4_USB_ELEMENT);

			m_NetworkProperties.ParseXml(xml);

			DialOutEnabled = XmlUtils.TryReadChildElementContentAsBoolean(xml, DIAL_OUT_ENABLED_ELEMENT) ?? DEFAULT_DIAL_OUT_ENABLED;

			RecordEnabled = XmlUtils.TryReadChildElementContentAsBoolean(xml, RECORD_ENABLED_ELEMENT) ?? DEFAULT_RECORD_ENABLED;

			MuteMyCameraOnStart = XmlUtils.TryReadChildElementContentAsBoolean(xml, MUTE_MY_CAMERA_ON_START_ELEMENT) ?? false;

			MuteParticipantsOnStart = XmlUtils.TryReadChildElementContentAsBoolean(xml, MUTE_PARTICIPANTS_ON_START_ELEMENT) ?? false;

			UpdateNetworkDefaults();
		}

		/// <summary>
		/// Sets default values for unconfigured network properties.
		/// </summary>
		private void UpdateNetworkDefaults()
		{
			m_NetworkProperties.ApplyDefaultValues(null, 2244, "zoom", "zoomus123");
		}
	}
}