using System;
using ICD.Common.Utils.Xml;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings
{
	public sealed class BookingCall : IEquatable<BookingCall>
	{
		public string Number { get; private set; }
		public string Protocol { get; private set; }
		public int CallRate { get; private set; }
		public eCiscoCallType CiscoCallType { get; private set; }

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
			eCiscoCallType ciscoCallType = XmlUtils.TryReadChildElementContentAsEnum<eCiscoCallType>(xml, "CallType", true) ??
			                     eCiscoCallType.Unknown;

			return new BookingCall
			{
				Number = number,
				Protocol = protocol,
				CallRate = callRate,
				CiscoCallType = ciscoCallType
			};
		}

		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
		public bool Equals(BookingCall other)
		{
			if (ReferenceEquals(null, other))
				return false;

			if (ReferenceEquals(this, other))
				return true;

			return string.Equals(Number, other.Number) &&
			       string.Equals(Protocol, other.Protocol) &&
			       CallRate == other.CallRate &&
			       CiscoCallType == other.CiscoCallType;
		}

		/// <summary>Determines whether the specified object is equal to the current object.</summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;

			if (ReferenceEquals(this, obj))
				return true;

			return obj is BookingCall && Equals((BookingCall)obj);
		}

		/// <summary>Serves as the default hash function.</summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = (Number != null ? Number.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ (Protocol != null ? Protocol.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^ CallRate;
				hashCode = (hashCode * 397) ^ (int)CiscoCallType;
				return hashCode;
			}
		}
	}
}