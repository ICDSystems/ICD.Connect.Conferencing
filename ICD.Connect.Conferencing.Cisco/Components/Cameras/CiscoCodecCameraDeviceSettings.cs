using System;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Cameras;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class CiscoCodecCameraDeviceSettings : AbstractCameraDeviceSettings
	{
		private const string FACTORY_NAME = "CiscoCamera";

		private const string PAN_TILT_SPEED_ELEMENT = "PanTiltSpeed";
		private const string ZOOM_SPEED_ELEMENT = "ZoomSpeed";
		private const string CODEC_ID_ELEMENT = "Codec";
		private const string CAMERA_ID_ELEMENT = "CameraId";

		private int? m_PanTiltSpeed;
		private int? m_ZoomSpeed;

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(CiscoCodecCameraDevice); } }

		[OriginatorIdSettingsProperty(typeof(CiscoCodec))]
		public int? CodecId { get; set; }

		public int? CameraId { get; set; }

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		public int? PanTiltSpeed
		{
			get { return m_PanTiltSpeed; }
			set
			{
				if (value == null)
				{
					m_PanTiltSpeed = null;
				}
				else
				{
					m_PanTiltSpeed = MathUtils.Clamp(value.Value, 1, 15);
				}
			}
		}

		public int? ZoomSpeed
		{
			get { return m_ZoomSpeed; }
			set
			{
				if (value == null)
				{
					m_ZoomSpeed = null;
				}
				else
				{
					m_ZoomSpeed = MathUtils.Clamp(value.Value, 1, 15);
				}
			}
		}

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(CODEC_ID_ELEMENT, IcdXmlConvert.ToString(CodecId));
			writer.WriteElementString(CAMERA_ID_ELEMENT, IcdXmlConvert.ToString(CameraId));
			writer.WriteElementString(PAN_TILT_SPEED_ELEMENT, IcdXmlConvert.ToString(PanTiltSpeed));
			writer.WriteElementString(ZOOM_SPEED_ELEMENT, IcdXmlConvert.ToString(ZoomSpeed));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			CodecId = XmlUtils.TryReadChildElementContentAsInt(xml, CODEC_ID_ELEMENT);
			CameraId = XmlUtils.TryReadChildElementContentAsInt(xml, CAMERA_ID_ELEMENT);
			PanTiltSpeed = XmlUtils.TryReadChildElementContentAsInt(xml, PAN_TILT_SPEED_ELEMENT);
			ZoomSpeed = XmlUtils.TryReadChildElementContentAsInt(xml, ZOOM_SPEED_ELEMENT);
		}
	}
}