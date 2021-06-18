using ICD.Common.Utils.Xml;
using ICD.Connect.Audio.VolumePoints;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Devices.Points;

namespace ICD.Connect.Conferencing.ConferencePoints
{
	public abstract class AbstractConferencePointSettings : AbstractPointSettings, IConferencePointSettings
	{
		private const string TYPE_ELEMENT = "Type";
		private const string PRIVACY_MUTE_MASK_ELEMENT = "PrivacyMuteMask";

		#region Properties

		/// <summary>
		/// The type of call to use the conference control for.
		/// </summary>
		public eCallType Type { get; set; }

		/// <summary>
		/// Determines if the privacy mute control will be driven by the control system, and/or drive the control system.
		/// </summary>
		public ePrivacyMuteFeedback PrivacyMuteMask { get; set; }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractConferencePointSettings()
		{
			PrivacyMuteMask = ePrivacyMuteFeedback.Set;
		}

		#region Methods

		/// <summary>
		/// Write property elements to xml
		/// </summary>
		/// <param name="writer"></param>
		protected override void WriteElements(IcdXmlTextWriter writer)
		{
			base.WriteElements(writer);

			writer.WriteElementString(TYPE_ELEMENT, IcdXmlConvert.ToString(Type));
			writer.WriteElementString(PRIVACY_MUTE_MASK_ELEMENT, IcdXmlConvert.ToString(PrivacyMuteMask));
		}

		/// <summary>
		/// Instantiate conference point settings from an xml element
		/// </summary>
		/// <param name="xml"></param>
		public override void ParseXml(string xml)
		{
			base.ParseXml(xml);

			Type = XmlUtils.TryReadChildElementContentAsEnum<eCallType>(xml, TYPE_ELEMENT, true) ?? eCallType.Unknown;

			PrivacyMuteMask =
				XmlUtils.TryReadChildElementContentAsEnum<ePrivacyMuteFeedback>(xml, PRIVACY_MUTE_MASK_ELEMENT, true) ??
				ePrivacyMuteFeedback.Set;
		}

		#endregion
	}
}
