using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Bookings;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.EventArguments;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components.Bookings
{
	[TestFixture]
	public sealed class BookingCallTest
	{
		[Test]
		public void FromXmlTest()
		{
			const string xml = @"        <Call item=""1"" maxOccurrence=""n"">
          <Number>432@firstrepublic.com</Number>
          <Protocol>SIP</Protocol>
          <CallRate>4096</CallRate>
          <CallType>Video</CallType>
        </Call>";

			BookingCall info = BookingCall.FromXml(xml);

			Assert.AreEqual("432@firstrepublic.com", info.Number);
			Assert.AreEqual("SIP", info.Protocol);
			Assert.AreEqual(4096, info.CallRate);
			Assert.AreEqual(eCallType.Video, info.CiscoCallType);
		}
	}
}
