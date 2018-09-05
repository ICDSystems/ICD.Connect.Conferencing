using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings
{
	public sealed class BookingCall
	{
		public string Number { get; private set; }
		public string Protocol { get; private set; }
		public int CallRate { get; private set; }
		public eCallType CallType { get; private set; }

		/// <summary>
		/// Deserializes the given xml to a BookingCall instance.
		/// </summary>
		/// <param name="xml"></param>
		/// <returns></returns>
		public static BookingCall FromXml(string xml)
		{
			string number = XmlUtils.TryReadChildElementContentAsString(xml, "Number");
			string protocol = XmlUtils.TryReadChildElementContentAsString(xml, "Protocol");
			int callRate = XmlUtils.TryReadChildElementContentAsInt(xml, "CallRate") ?? 0;
			eCallType callType = XmlUtils.TryReadChildElementContentAsEnum<eCallType>(xml, "CallType", true) ??
			                     eCallType.Unknown;

			return new BookingCall
			{
				Number = number,
				Protocol = protocol,
				CallRate = callRate,
				CallType = callType
			};
		}
	}
}