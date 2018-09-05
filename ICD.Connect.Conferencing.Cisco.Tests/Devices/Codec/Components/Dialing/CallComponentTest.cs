using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Dialing;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components.Dialing
{
	[TestFixture]
	public sealed class CallComponentTest : AbstractCiscoComponentTest
	{
		[Test]
		public void StatusChangeFeedbackTest()
		{
			int id = 67;

			CallComponent component = new CallComponent(id, Codec);

			List<ConferenceSourceStatusEventArgs> responses = new List<ConferenceSourceStatusEventArgs>();

			component.OnStatusChanged += (sender, e) => responses.Add(e);

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
			Port.Receive(connecting);
			Port.Receive(connected);

			Port.Receive(ringingBad);
			Port.Receive(connectingBad);
			Port.Receive(connectedBad);

			Assert.AreEqual(3, responses.Count);
			Assert.AreEqual(eConferenceSourceStatus.Ringing, responses[0].Data);
			Assert.AreEqual(eConferenceSourceStatus.Connecting, responses[1].Data);
			Assert.AreEqual(eConferenceSourceStatus.Connected, responses[2].Data);
			Assert.AreEqual(eConferenceSourceStatus.Connected, component.Status);
		}

		[Test]
		public void DurationTest()
		{
			const int id = 67;

			CallComponent component = new CallComponent(id, Codec);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Call item=\"{0}\">"
				+ "<Status item=\"1\">{1}</Status>"
				+ "</Call>"
				+ "</Status>"
				+ "</XmlDoc>";

			string connected = string.Format(rX, id, "Connected");
			string disconnected = string.Format(rX, id, "Disconnected");

			Assert.AreEqual(0, component.GetDuration().Milliseconds);

			Port.Receive(connected);
			Assert.That(component.Start, Is.EqualTo(IcdEnvironment.GetLocalTime()).Within(1).Seconds);

			ThreadingUtils.Sleep(1000);

			// Sometimes the duration rounds to the next second.
			Assert.That(component.GetDuration().TotalMilliseconds, Is.EqualTo(1000).Within(1000));

			Port.Receive(disconnected);
			Assert.That(component.End, Is.EqualTo(IcdEnvironment.GetLocalTime()).Within(1).Seconds);

			Assert.That(component.GetDuration().TotalMilliseconds, Is.EqualTo(1000).Within(1000));
		}
	}
}
