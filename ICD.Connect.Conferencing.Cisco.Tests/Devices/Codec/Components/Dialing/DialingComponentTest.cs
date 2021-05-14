using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.EventArguments;
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

		[Test]
		public void StatusChangeFeedbackTest()
		{
			int id = 67;

			DialingComponent component = new DialingComponent(Codec);

			List<CallStatus> calls = new List<CallStatus>();

			component.OnSourceAdded += (sender, e) => calls.Add(e.Data);

			string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Call item=\"{0}\">"
				+ "<AnswerState item=\"1\">Unanswered</AnswerState>"
				+ "<CallPriority item=\"1\">None</CallPriority>"
				+ "<CallType item=\"1\">Video</CallType>"
				+ "<CallbackNumber item=\"1\">sip:115@ucm1.internal.profound-tech.com</CallbackNumber>"
				+ "<DeviceType item=\"1\">Endpoint</DeviceType>"
				+ "<Direction item=\"1\">Outgoing</Direction>"
				+ "<DisplayName item=\"1\"></DisplayName>"
				+ "<Duration item=\"1\" func_get=\"2\" func_item=\"67\">1356524</Duration>"
				+ "<Encryption item=\"1\">"
				+ "<Type item=\"1\">Aes-128</Type>"
				+ "</Encryption>"
				+ "<FacilityServiceId item=\"1\">0</FacilityServiceId>"
				+ "<ModifyState item=\"1\">Idle</ModifyState>"
				+ "<PlacedOnHold item=\"1\">False</PlacedOnHold>"
				+ "<Protocol item=\"1\">sip</Protocol>"
				+ "<ReceiveCallRate item=\"1\">6000</ReceiveCallRate>"
				+ "<RemoteNumber item=\"1\">115@ucm1.internal.profound-tech.com</RemoteNumber>"
				+ "<Status item=\"1\">{1}</Status>"
				+ "<TransmitCallRate item=\"1\">6000</TransmitCallRate>"
				+ "</Call>"
				+ "</Status>"
				+ "</XmlDoc>";

			string ringing = string.Format(rX, id, "Ringing");
			string connecting = string.Format(rX, id, "Connecting");
			string connected = string.Format(rX, id, "Connected");

			string ringingBad = string.Format(rX, 12, "Ringing");
			string connectingBad = string.Format(rX, 37, "Connecting");
			string connectedBad = string.Format(rX, 2, "Connected");

			Port.Receive(ringing);
			Assert.AreEqual(eParticipantStatus.Ringing, calls[0].Status);

			Port.Receive(connecting);
			Assert.AreEqual(eParticipantStatus.Connecting, calls[0].Status);

			Port.Receive(connected);
			Assert.AreEqual(eParticipantStatus.Connected, calls[0].Status);

			Port.Receive(ringingBad);
			Port.Receive(connectingBad);
			Port.Receive(connectedBad);

			Assert.AreEqual(4, calls.Count);
			Assert.AreEqual(eParticipantStatus.Connected, calls[0].Status);
		}
	}
}
