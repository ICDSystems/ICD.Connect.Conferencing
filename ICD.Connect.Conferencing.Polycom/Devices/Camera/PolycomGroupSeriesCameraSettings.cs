using ICD.Common.Utils.Xml;
using ICD.Connect.Cameras.Devices;
using ICD.Connect.Conferencing.Polycom.Devices.Codec;
using ICD.Connect.Settings.Attributes;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.Polycom.Devices.Camera
{
	[KrangSettings("PolycomGroupSeriesCamera", typeof(PolycomGroupSeriesCameraDevice))]
	public sealed class PolycomGroupSeriesCameraSettings : AbstractCameraDeviceSettings
	{
		private const string CODEC_ID_ELEMENT = "Codec";
		private const string CAMERA_ID_ELEMENT = "CameraId";

		[OriginatorIdSettingsProperty(typeof(PolycomGroupSeriesDevice))]
		public int? CodecId { get; set; }

		public int? CameraId { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(CODEC_ID_ELEMENT, IcdXmlConvert.ToString(CodecId));
			writer.WriteElementString(CAMERA_ID_ELEMENT, IcdXmlConvert.ToString(CameraId));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			CodecId = XmlUtils.TryReadChildElementContentAsInt(xml, CODEC_ID_ELEMENT);
			CameraId = XmlUtils.TryReadChildElementContentAsInt(xml, CAMERA_ID_ELEMENT) ?? 1;
		}
	}
}
