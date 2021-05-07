using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Controls.Conference;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components.Bookings
{
	[TestFixture]
	public sealed class BookingCallTest
	{
		private static readonly object[] s_FromXmlTestCaseOldFormat =
		{
			new object[]
			{
				@"        <Call item=""1"" maxOccurrence=""n"">
          <Number>432@firstrepublic.com</Number>
          <Protocol>SIP</Protocol>
          <CallRate>4096</CallRate>
          <CallType>Video</CallType>
        </Call>",
				"432@firstrepublic.com",
				"SIP",
				4096,
				eCiscoCallType.Video
			}
		};

		private static readonly object[] s_FromXmlTestCaseNewFormat =
		{
			new object[]
			{
				@"						<Call item=""1"" maxOccurrence=""n"">
						<Number>1457559328@onemetlife.webex.com</Number>
						<Protocol>Spark</Protocol>
					</Call>",
				"1457559328@onemetlife.webex.com",
				"Spark",
				0,
				eCiscoCallType.Unknown
			}
		};

		[TestCaseSource(nameof(s_FromXmlTestCaseOldFormat))]
		[TestCaseSource(nameof(s_FromXmlTestCaseNewFormat))]
		public void FromXmlTest(string xml, string expectedNumber, string expectedProtocol, int expectedCallRate, eCiscoCallType expectedCallType)
		{
			BookingCall info = BookingCall.FromXml(xml);

			Assert.AreEqual(expectedNumber, info.Number);
			Assert.AreEqual(expectedProtocol, info.Protocol);
			Assert.AreEqual(expectedCallRate, info.CallRate);
			Assert.AreEqual(expectedCallType, info.CiscoCallType);
		}
	}
}
