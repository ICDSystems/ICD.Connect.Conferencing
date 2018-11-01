using ICD.Connect.Conferencing.Utils;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Tests.Utils
{
	[TestFixture]
	public sealed class SipUtilsTest
	{
		[TestCase("sip:1000@10.31.1.29:5060", "1000")]
		public void NumberFromUriTest(string uri, string expected)
		{
			Assert.AreEqual(expected, SipUtils.NumberFromUri(uri));
		}

		[TestCase("sip:jeffery@wedoresi.com", true)]
		[TestCase("sip:1234567890", true)]
		[TestCase("\"100\"", false)]
		public void IsValidNumberTest(string number, bool expected)
		{
			Assert.AreEqual(expected, SipUtils.IsValidSipUri(number));
		}
	}
}
