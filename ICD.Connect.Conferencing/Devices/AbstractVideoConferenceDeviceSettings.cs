using ICD.Common.Utils.Xml;
using ICD.Connect.Devices;

namespace ICD.Connect.Conferencing.Devices
{
	public abstract class AbstractVideoConferenceDeviceSettings : AbstractDeviceSettings, IVideoConferenceDeviceSettings
	{
		private const string INPUT_1_ELEMENT = "Input1Type";
		private const string INPUT_2_ELEMENT = "Input2Type";
		private const string INPUT_3_ELEMENT = "Input3Type";
		private const string INPUT_4_ELEMENT = "Input4Type";
		private const string INPUT_5_ELEMENT = "Input5Type";
		private const string INPUT_6_ELEMENT = "Input6Type";

		public eCodecInputType Input1CodecInputType { get; set; }
		public eCodecInputType Input2CodecInputType { get; set; }
		public eCodecInputType Input3CodecInputType { get; set; }
		public eCodecInputType Input4CodecInputType { get; set; }
		public eCodecInputType Input5CodecInputType { get; set; }
		public eCodecInputType Input6CodecInputType { get; set; }

		/// <summary>
		/// Writes property elements to xml.
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(INPUT_1_ELEMENT, IcdXmlConvert.ToString(Input1CodecInputType));
			writer.WriteElementString(INPUT_2_ELEMENT, IcdXmlConvert.ToString(Input2CodecInputType));
			writer.WriteElementString(INPUT_3_ELEMENT, IcdXmlConvert.ToString(Input3CodecInputType));
			writer.WriteElementString(INPUT_4_ELEMENT, IcdXmlConvert.ToString(Input4CodecInputType));
			writer.WriteElementString(INPUT_5_ELEMENT, IcdXmlConvert.ToString(Input5CodecInputType));
			writer.WriteElementString(INPUT_6_ELEMENT, IcdXmlConvert.ToString(Input6CodecInputType));
		}

		/// <summary>
		/// Updates the settings from xml.
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Input1CodecInputType = XmlUtils.TryReadChildElementContentAsEnum<eCodecInputType>(xml, INPUT_1_ELEMENT, true) ??
			                       eCodecInputType.None;
			Input2CodecInputType = XmlUtils.TryReadChildElementContentAsEnum<eCodecInputType>(xml, INPUT_2_ELEMENT, true) ??
			                       eCodecInputType.None;
			Input3CodecInputType = XmlUtils.TryReadChildElementContentAsEnum<eCodecInputType>(xml, INPUT_3_ELEMENT, true) ??
			                       eCodecInputType.None;
			Input4CodecInputType = XmlUtils.TryReadChildElementContentAsEnum<eCodecInputType>(xml, INPUT_4_ELEMENT, true) ??
			                       eCodecInputType.None;
			Input5CodecInputType = XmlUtils.TryReadChildElementContentAsEnum<eCodecInputType>(xml, INPUT_5_ELEMENT, true) ??
			                       eCodecInputType.None;
			Input6CodecInputType = XmlUtils.TryReadChildElementContentAsEnum<eCodecInputType>(xml, INPUT_6_ELEMENT, true) ??
			                       eCodecInputType.None;
		}
	}
}
