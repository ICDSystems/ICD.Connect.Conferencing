using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices;
using ICD.Connect.Settings;
using ICD.Connect.Settings.Attributes.SettingsProperties;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public abstract class AbstractConferencePointSettings : AbstractSettings, IConferencePointSettings
	{
		private const string DEVICE_ELEMENT = "Device";
		private const string CONTROL_ELEMENT = "Control";
		private const string TYPE_ELEMENT = "Type";

		#region Properties

		/// <summary>
		/// Device id for this volume point
		/// </summary>
		[OriginatorIdSettingsProperty(typeof(IDeviceBase))]
		public int DeviceId { get; set; }

		/// <summary>
		/// Control id for an IVolumeControl on this volume point's device
		/// </summary>
		public int ControlId { get; set; }

		public eCallType Type { get; set; }

		#endregion

		#region Methods

		/// <summary>
		/// Write property elements to xml
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(DEVICE_ELEMENT, IcdXmlConvert.ToString(DeviceId));
			writer.WriteElementString(CONTROL_ELEMENT, IcdXmlConvert.ToString(ControlId));
			writer.WriteElementString(TYPE_ELEMENT, IcdXmlConvert.ToString(Type));
		}

		/// <summary>
		/// Instantiate volume point settings from an xml element
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			DeviceId = XmlUtils.TryReadChildElementContentAsInt(xml, DEVICE_ELEMENT) ?? 0;
			ControlId = XmlUtils.TryReadChildElementContentAsInt(xml, CONTROL_ELEMENT) ?? 0;
			Type = XmlUtils.TryReadChildElementContentAsEnum<eCallType>(xml, TYPE_ELEMENT, true) ?? eCallType.Unknown;
		}

		#endregion
	}
}
