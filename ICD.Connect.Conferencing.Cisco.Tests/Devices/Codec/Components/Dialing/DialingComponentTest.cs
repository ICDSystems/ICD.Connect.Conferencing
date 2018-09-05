using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components.Dialing
{
	[TestFixture]
	public sealed class DialingComponentTest : AbstractCiscoComponentTest
	{
		[Test]
		public void PrivacyMuteFeedbackTest()
		{
			DialingComponent component = new DialingComponent(Codec);

			List<BoolEventArgs> responses = new List<BoolEventArgs>();

			component.OnPrivacyMuteChanged += (sender, e) => responses.Add(e);

			string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Audio item=\"1\">"
				+ "<Microphones item=\"1\">"
				+ "<Mute item=\"1\">{0}</Mute>"
				+ "</Microphones>"
				+ "</Audio>"
				+ "</Status>"
				+ "</XmlDoc>";

			string active = string.Format(rX, "On");
			string inactive = string.Format(rX, "Off");

			Port.Receive(active);
			Port.Receive(active);
			Port.Receive(inactive);
			Port.Receive(active);

			Assert.AreEqual(3, responses.Count);
			Assert.IsTrue(responses[0].Data);
			Assert.IsFalse(responses[1].Data);
			Assert.IsTrue(responses[2].Data);
			Assert.IsTrue(component.PrivacyMuted);
		}

		[Test]
		public void DoNotDisturbStateTest()
		{
			DialingComponent component = new DialingComponent(Codec);

			List<BoolEventArgs> responses = new List<BoolEventArgs>();

			component.OnDoNotDisturbChanged += (sender, e) => responses.Add(e);

			string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Conference item=\"1\">"
				+ "<DoNotDisturb item=\"1\">{0}</DoNotDisturb>"
				+ "</Conference>"
				+ "</Status>"
				+ "</XmlDoc>";

			string active = string.Format(rX, "Active");
			string inactive = string.Format(rX, "Inactive");

			Port.Receive(active);
			Port.Receive(active);
			Port.Receive(inactive);
			Port.Receive(active);

			Assert.AreEqual(3, responses.Count);
			Assert.IsTrue(responses[0].Data);
			Assert.IsFalse(responses[1].Data);
			Assert.IsTrue(responses[2].Data);
			Assert.IsTrue(component.DoNotDisturb);
		}

		[Test]
		public void AutoAnswerStatTest()
		{
			DialingComponent component = new DialingComponent(Codec);

			List<BoolEventArgs> responses = new List<BoolEventArgs>();

			component.OnAutoAnswerChanged += (sender, e) => responses.Add(e);

			string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Configuration>"
				+ "<Conference item=\"1\" maxOccurrence=\"1\">"
				+ "<AutoAnswer item=\"1\">"
				+ "<Mode item=\"1\" valueSpaceRef=\"/Valuespace/TTPAR_OnOff\">{0}</Mode>"
				+ "</AutoAnswer>"
				+ "</Conference>"
				+ "</Configuration>"
				+ "</XmlDoc>";

			string active = string.Format(rX, "On");
			string inactive = string.Format(rX, "Off");

			Port.Receive(active);
			Port.Receive(active);
			Port.Receive(inactive);
			Port.Receive(active);

			Assert.AreEqual(3, responses.Count);
			Assert.IsTrue(responses[0].Data);
			Assert.IsFalse(responses[1].Data);
			Assert.IsTrue(responses[2].Data);
			Assert.IsTrue(component.AutoAnswer);
		}
	}
}
