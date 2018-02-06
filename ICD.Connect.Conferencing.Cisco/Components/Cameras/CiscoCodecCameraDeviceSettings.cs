using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Xml;
using ICD.Connect.Cameras;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	public sealed class CiscoCodecCameraDeviceSettings : AbstractCameraDeviceSettings
	{
		private int? m_PanTiltSpeed;
		private int? m_ZoomSpeed;
		private const string FACTORY_NAME = "CiscoCamera";
		private const string PAN_TILT_SPEED_ELEMENT = "PanTiltSpeed";
		private const string ZOOM_SPEED_ELEMENT = "ZoomSpeed";
		private const string CODEC_ID_ELEMENT = "Codec";
		private const string CAMERA_ID_ELEMENT = "CameraId";

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
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(CiscoCodecCameraDevice); } }

		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static CiscoCodecCameraDeviceSettings FromXml(string xml)
		{
			CiscoCodecCameraDeviceSettings output = new CiscoCodecCameraDeviceSettings();
			ParseXml(output, xml);
			return output;
		}

		private static void ParseXml(CiscoCodecCameraDeviceSettings instance, string xml)
		{
			instance.CodecId = XmlUtils.TryReadChildElementContentAsInt(xml, CODEC_ID_ELEMENT);
			instance.CameraId = XmlUtils.TryReadChildElementContentAsInt(xml, CAMERA_ID_ELEMENT);
			instance.PanTiltSpeed = XmlUtils.TryReadChildElementContentAsInt(xml, PAN_TILT_SPEED_ELEMENT);
			instance.ZoomSpeed = XmlUtils.TryReadChildElementContentAsInt(xml, ZOOM_SPEED_ELEMENT);
			AbstractCameraDeviceSettings.ParseXml(instance, xml);
		}
	}
}