using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings
{
	public sealed class DialInfo
	{
		public int Id { get; private set; }
		public string Number { get; private set; }
		//public eProtocol Protocol { get; private set; }
		public int CallRate { get; private set; }
		public eCallType CallType { get; private set; }

		/// <summary>
		/// Deserializes the given xml to a DialInfo instance.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static DialInfo FromXml(string xml)
		{
			int id = XmlUtils.GetAttributeAsInt(xml, "id");
			string number = XmlUtils.TryReadChildElementContentAsString(xml, "Number");
			int callRate = XmlUtils.TryReadChildElementContentAsInt(xml, "CallRate") ?? 0;
			eCallType callType = XmlUtils.TryReadChildElementContentAsEnum<eCallType>(xml, "CallType", true) ??
			                     eCallType.Unknown;

			return new DialInfo
			{
				Id = id,
				Number = number,
				CallRate = callRate,
				CallType = callType
			};
		}
	}
}