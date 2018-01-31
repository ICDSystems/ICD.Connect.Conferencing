using System;
using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Cameras;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Cisco.Components.Cameras
{
	public sealed class CiscoCodecCameraDeviceSettings : AbstractDeviceSettings, ICameraDeviceSettings
	{
		private const string FACTORY_NAME = "CiscoCamera";

		private const string CODEC_ID_ELEMENT = "Codec";
		private const string CAMERA_ID_ELEMENT = "CameraId";

		public int? CodecId { get; set; }

		public int? CameraId { get; set; }

		/// <summary>
		/// Gets the originator factory name.
		/// </summary>
		public override string FactoryName { get { return FACTORY_NAME; } }

		/// <summary>
		/// Gets the type of the originator for this settings instance.
		/// </summary>
		public override Type OriginatorType { get { return typeof(CiscoCodecCameraDevice); } }

		[PublicAPI, XmlFactoryMethod(FACTORY_NAME)]
		public static CiscoCodecCameraDeviceSettings FromXml(string xml)
		{
			CiscoCodecCameraDeviceSettings output = new CiscoCodecCameraDeviceSettings()
			{
				CodecId = XmlUtils.TryReadChildElementContentAsInt(xml, CODEC_ID_ELEMENT),
				CameraId = XmlUtils.TryReadChildElementContentAsInt(xml, CAMERA_ID_ELEMENT)
			}; ;
			ParseXml(output, xml);
			return output;
		}
	}
}