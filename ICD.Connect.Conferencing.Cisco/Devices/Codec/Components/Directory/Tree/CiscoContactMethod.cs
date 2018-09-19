using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Directory.Tree
{
	/// <summary>
	/// ContactMethod stores contact method info.
	/// </summary>
	[XmlConverter(typeof(CiscoContactMethodXmlConverter))]
	public sealed class CiscoContactMethod : IContactMethod
	{
		/// <summary>
		/// Gets the ContactMethodId.
		/// </summary>
		[PublicAPI]
		public int ContactMethodId { get; set; }

		/// <summary>
		/// Gets the contact number.
		/// </summary>
		[PublicAPI]
		public string Number { get; set; }

		/// <summary>
		/// Gets the call type.
		/// </summary>
		[PublicAPI]
		public eCallType CallType { get; set; }
	}

	public sealed class CiscoContactMethodXmlConverter : AbstractGenericXmlConverter<CiscoContactMethod>
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
		protected override CiscoContactMethod Instantiate()
		{
			return new CiscoContactMethod();
		}

		/// <summary>
		/// Override to handle the current element.
		/// </summary>
		/// <param name="reader"></param>
		/// <param name="instance"></param>
		protected override void ReadElement(IcdXmlReader reader, CiscoContactMethod instance)
		{
			switch (reader.Name)
			{
				case "ContactMethodId":
					instance.ContactMethodId = reader.ReadElementContentAsInt();
					break;

				case "Number":
					instance.Number = reader.ReadElementContentAsString();
					break;

				case "CallType":
					instance.CallType = reader.ReadElementContentAsEnum<eCallType>(true);
					break;

				default:
					base.ReadElement(reader, instance);
					break;
			}
		}
	}
}
