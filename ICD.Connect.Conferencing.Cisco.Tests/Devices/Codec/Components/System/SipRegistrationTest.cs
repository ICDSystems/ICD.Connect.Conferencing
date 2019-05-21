using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components.System
{
	[TestFixture]
	public sealed class SipRegistrationTest : AbstractCiscoComponentTest
	{
		[Test]
		public void ReasonChangeFeedbackTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void RegistrationChangeFeedbackTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void UriChangeFeedbackTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void ProxyAddressChangedFeedbackTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void ProxyStatusChangedFeedbackTest()
		{
			Assert.Inconclusive();
		}


		[Test]
		public void ItemTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void ReasonTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void RegistrationTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void UriTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void ProxyAddressTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void ProxyStatusTest()
		{
			Assert.Inconclusive();
		}

		[Test]
		public void ParseXmlTest()
		{
			const string proxyXml = @"<Proxy item=""1"" maxOccurrence=""n"">
	<Address>10.218.127.81</Address>
	<Status>Active</Status>
</Proxy>";

			const string registrationXml = @"<Registration item=""1"" maxOccurrence=""n"">
	<Reason></Reason>
	<Status>Registered</Status>
	<URI>26121340@metlife.com</URI>
</Registration>";

			SipRegistration registration = new SipRegistration(Codec, 1);
			registration.ParseXml(proxyXml);
			registration.ParseXml(registrationXml);

			Assert.AreEqual("10.218.127.81", registration.ProxyAddress);
			Assert.AreEqual("Active", registration.ProxyStatus);
			Assert.AreEqual("", registration.Reason);
			Assert.AreEqual(eRegState.Registered, registration.Registration);
			Assert.AreEqual("26121340@metlife.com", registration.Uri);
		}
	}
}
