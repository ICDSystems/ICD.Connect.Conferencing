using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference;
using ICD.Connect.Conferencing.DialContexts;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree
{
	/// <summary>
	/// ContactMethod stores contact method info.
	/// </summary>
	[XmlConverter(typeof(CiscoContactMethodXmlConverter))]
	public sealed class CiscoContactDialContext : AbstractDialContext
	{
		/// <summary>
		/// Gets the ContactMethodId.
		/// </summary>
		[PublicAPI]
		public int ContactMethodId { get; set; }
	}

	public sealed class CiscoContactMethodXmlConverter : AbstractGenericXmlConverter<CiscoContactDialContext>
	{
		// <ContactMethod item="1">
		//   <ContactMethodId item="1">1</ContactMethodId>
		//   <Number item="1">112</Number>
		//   <CallType item="1">Video</CallType>
		// </ContactMethod>

		/// <summary>
		/// Creates a new instance of T.
		/// </summary>
		/// <returns></returns>
		protected override CiscoContactDialContext Instantiate()
		{
			return new CiscoContactDialContext();
		}

		/// <summary>
		/// Override to handle the current element.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		protected override void ReadElement(IcdXmlReader reader, CiscoContactDialContext instance)
		{
			switch (reader.Name)
			{
				case "ContactMethodId":
					instance.ContactMethodId = reader.ReadElementContentAsInt();
					break;

				case "Number":
					instance.DialString = reader.ReadElementContentAsString();
					break;

				case "CallType":
					instance.CallType = reader.ReadElementContentAsEnum<eCiscoCallType>(true).ToCallType();
					break;

				default:
					base.ReadElement(reader, instance);
					break;
			}
		}
	}
}
