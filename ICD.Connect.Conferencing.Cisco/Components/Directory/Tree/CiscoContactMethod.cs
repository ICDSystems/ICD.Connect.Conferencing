using ICD.Common.Properties;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Contacts;

namespace ICD.Connect.Conferencing.Cisco.Components.Directory.Tree
{
	/// <summary>
	/// ContactMethod stores contact method info.
	/// </summary>
	public sealed class CiscoContactMethod : IContactMethod
	{
		private readonly string m_ContactMethodId;
		private readonly string m_Number;

		#region Properties

		/// <summary>
		/// Gets the ContactMethodId.
		/// </summary>
		[PublicAPI]
		public string ContactMethodId { get { return m_ContactMethodId; } }

		/// <summary>
		/// Gets the contact number.
		/// </summary>
		[PublicAPI]
		public string Number { get { return m_Number; } }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="contactMethodId"></param>
		/// <param name="number"></param>
		private CiscoContactMethod(string contactMethodId, string number)
		{
			m_ContactMethodId = contactMethodId;
			m_Number = number;
		}

		/// <summary>
		/// Creates a ContactMethod from a ContactMethod XML Element.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static CiscoContactMethod FromXml(string xml)
		{
			string contactMethodId = XmlUtils.ReadChildElementContentAsString(xml, "ContactMethodId");
			string number = XmlUtils.ReadChildElementContentAsString(xml, "Number");
			return new CiscoContactMethod(contactMethodId, number);
		}

		#endregion
	}
}
