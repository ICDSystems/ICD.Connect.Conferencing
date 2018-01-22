using NUnit.Framework;
using ICD.Connect.Conferencing.Cisco.Components.Diagnostics;

namespace ICD.Connect.Conferencing.Cisco.Tests.Components.Diagnostics
{
	[TestFixture]
	public sealed class DiagnosticsMessageTest
	{
		[Test]
		public void FromXmlTest()
		{
			const string xml = "<Message item=\"8\" maxOccurrence=\"n\">"
			                   + "<Description>High delay in audio loop detected. Highest delay: 190ms</Description>"
			                   + "<Level>Warning</Level>"
			                   + "<References>delay=190</References>"
			                   + "<Type>ECReferenceDelay</Type>"
			                   + "</Message>";

			DiagnosticsMessage message = DiagnosticsMessage.FromXml(xml);
			DiagnosticsMessage expected = new DiagnosticsMessage("High delay in audio loop detected. Highest delay: 190ms",
			                                                     DiagnosticsMessage.eLevel.Warning, "delay=190",
			                                                     "ECReferenceDelay");

			Assert.AreEqual(expected, message);
		}

		[Test]
		public void EqualityTest()
		{
			DiagnosticsMessage test1 = new DiagnosticsMessage("", DiagnosticsMessage.eLevel.Warning, "Test", null);
			DiagnosticsMessage test2 = new DiagnosticsMessage("Test", DiagnosticsMessage.eLevel.Error, null, "");

			// ReSharper disable once EqualExpressionComparison
			Assert.IsTrue(test1 == test1);
			Assert.IsFalse(test1 == test2);
		}

		[Test]
		public void InequalityTest()
		{
			DiagnosticsMessage test1 = new DiagnosticsMessage("", DiagnosticsMessage.eLevel.Warning, "Test", null);
			DiagnosticsMessage test2 = new DiagnosticsMessage("Test", DiagnosticsMessage.eLevel.Error, null, "");

			// ReSharper disable once EqualExpressionComparison
			Assert.IsFalse(test1 != test1);
			Assert.IsTrue(test1 != test2);
		}

		[Test]
		public void EqualsTest()
		{
			DiagnosticsMessage test1 = new DiagnosticsMessage("", DiagnosticsMessage.eLevel.Warning, "Test", null);
			DiagnosticsMessage test2 = new DiagnosticsMessage("Test", DiagnosticsMessage.eLevel.Error, null, "");

			// ReSharper disable once EqualExpressionComparison
			Assert.IsTrue(test1.Equals(test1));
			Assert.IsFalse(test1.Equals(test2));
		}

		[Test]
		public void GetHashCodeTest()
		{
			DiagnosticsMessage test1 = new DiagnosticsMessage("", DiagnosticsMessage.eLevel.Warning, "Test", null);
			DiagnosticsMessage test2 = new DiagnosticsMessage("Test", DiagnosticsMessage.eLevel.Error, null, "");

			// ReSharper disable once EqualExpressionComparison
			Assert.IsTrue(test1.GetHashCode() == test1.GetHashCode());
			Assert.IsFalse(test1.GetHashCode() == test2.GetHashCode());
		}
	}
}