using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Points;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public abstract class AbstractConferencePointSettings : AbstractPointSettings, IConferencePointSettings
	{
		private const string TYPE_ELEMENT = "Type";

		#region Properties

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

			writer.WriteElementString(TYPE_ELEMENT, IcdXmlConvert.ToString(Type));
		}

		/// <summary>
		/// Instantiate conference point settings from an xml element
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Type = XmlUtils.TryReadChildElementContentAsEnum<eCallType>(xml, TYPE_ELEMENT, true) ?? eCallType.Unknown;
		}

		#endregion
	}
}
