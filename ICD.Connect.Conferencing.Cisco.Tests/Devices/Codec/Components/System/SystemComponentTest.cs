using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using NUnit.Framework;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;

namespace ICD.Connect.Conferencing.Cisco.Tests.Components.System
{
	[TestFixture]
	public sealed class SystemComponentTest : AbstractCiscoComponentTest
	{
		[Test]
		public void SipRegistrationStatusChangeFeedbackTest()
		{
			SystemComponent component = new SystemComponent(Codec);

			List<RegistrationEventArgs> responses = new List<RegistrationEventArgs>();

			component.OnSipRegistrationChange += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<SIP item=\"1\">"
				+ "<Registration item=\"1\">"
				+ "<Status item=\"1\">{0}</Status>"
				+ "</Registration>"
				+ "</SIP>"
				+ "</Status>"
				+ "</XmlDoc>";

			string failed = string.Format(rX, eRegState.Failed);
			string inactive = string.Format(rX, eRegState.Inactive);
			string registered = string.Format(rX, eRegState.Registered);

			Port.Receive(failed);
			Port.Receive(inactive);
			Port.Receive(registered);

			Assert.AreEqual(3, responses.Count);
			Assert.AreEqual(eRegState.Failed, responses[0].Data);
			Assert.AreEqual(eRegState.Inactive, responses[1].Data);
			Assert.AreEqual(eRegState.Registered, responses[2].Data);
			Assert.AreEqual(eRegState.Registered, component.SipRegistration);
		}

		[Test]
		public void GatekeeperStatusChangeFeedbackTest()
		{
			SystemComponent component = new SystemComponent(Codec);

			List<GatekeeperStatusArgs> responses = new List<GatekeeperStatusArgs>();

			component.OnGatekeeperStatusChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<H323 item=\"1\">"
				+ "<Gatekeeper item=\"1\">"
				+ "<Status item=\"1\">{0}</Status>"
				+ "</Gatekeeper>"
				+ "</H323>"
				+ "</Status>"
				+ "</XmlDoc>";

			string discovering = string.Format(rX, eH323GatekeeperStatus.Discovering);
			string registered = string.Format(rX, eH323GatekeeperStatus.Registered);
			string authenticating = string.Format(rX, eH323GatekeeperStatus.Authenticating);

			Port.Receive(discovering);
			Port.Receive(registered);
			Port.Receive(authenticating);

			Assert.AreEqual(3, responses.Count);
			Assert.AreEqual(eH323GatekeeperStatus.Discovering, responses[0].Data);
			Assert.AreEqual(eH323GatekeeperStatus.Registered, responses[1].Data);
			Assert.AreEqual(eH323GatekeeperStatus.Authenticating, responses[2].Data);
			Assert.AreEqual(eH323GatekeeperStatus.Authenticating, component.H323GatekeeperStatus);
		}

		[Test]
		public void AwakeStateChangedFeedbackTest()
		{
			SystemComponent component = new SystemComponent(Codec);

			List<BoolEventArgs> responses = new List<BoolEventArgs>();

			component.OnAwakeStateChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Standby>"
				+ "<State>{0}</State>"
				+ "</Standby>"
				+ "</Status>"
				+ "</XmlDoc>";

			string on = string.Format(rX, "On");
			string off = string.Format(rX, "Off");

			Port.Receive(on);
			Port.Receive(off);

			Assert.AreEqual(2, responses.Count);
			Assert.IsTrue(responses[0].Data);
			Assert.IsFalse(responses[1].Data);
			Assert.IsFalse(component.Awake);
		}

		[Test]
		public void NameChangedFeedbackTest()
		{
			SystemComponent component = new SystemComponent(Codec);

			List<StringEventArgs> responses = new List<StringEventArgs>();

			component.OnNameChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<UserInterface>"
				+ "<ContactInfo>"
				+ "<Name>{0}</Name>"
				+ "</ContactInfo>"
				+ "</UserInterface>"
				+ "</Status>"
				+ "</XmlDoc>";

			const string name = "foobar";

			string rX1 = string.Format(rX, name);

			Port.Receive(rX1);

			Assert.AreEqual(1, responses.Count);
			Assert.AreEqual(name, responses[0].Data);
			Assert.AreEqual(name, component.Name);
		}

		[Test]
		public void AddressChangedFeedbackTest()
		{
			SystemComponent component = new SystemComponent(Codec);

			List<StringEventArgs> responses = new List<StringEventArgs>();

			component.OnAddressChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Network item=\"1\">"
				+ "<IPv4 item=\"1\">"
				+ "<Address item=\"1\">{0}</Address>"
				+ "</IPv4>"
				+ "</Network>"
				+ "</Status>"
				+ "</XmlDoc>";

			const string address1 = "127.0.0.1";
			const string address2 = "foobar";

			string rX1 = string.Format(rX, address1);
			string rX2 = string.Format(rX, address2);

			Port.Receive(rX1);
			Port.Receive(rX2);

			Assert.AreEqual(2, responses.Count);
			Assert.AreEqual(address1, responses[0].Data);
			Assert.AreEqual(address2, responses[1].Data);
			Assert.AreEqual(address2, component.Address);
		}

		[Test]
		public void SoftwareVersionChangedFeedbackTest()
		{
			SystemComponent component = new SystemComponent(Codec);

			List<StringEventArgs> responses = new List<StringEventArgs>();

			component.OnSoftwareVersionChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<SystemUnit item=\"1\">"
				+ "<Software item=\"1\">"
				+ "<Version item=\"1\">{0}</Version>"
				+ "</Software>"
				+ "</SystemUnit>"
				+ "</Status>"
				+ "</XmlDoc>";

			const string version = "TC7.3.2.14ad7cc";

			string rX1 = string.Format(rX, version);

			Port.Receive(rX1);

			Assert.AreEqual(1, responses.Count);
			Assert.AreEqual(version, responses[0].Data);
			Assert.AreEqual(version, component.SoftwareVersion);
		}

		[Test]
		public void H323EnabledStateChangedFeedbackTest()
		{
			SystemComponent component = new SystemComponent(Codec);

			List<BoolEventArgs> responses = new List<BoolEventArgs>();

			component.OnH323EnabledStateChanged += (sender, e) => responses.Add(e);

			string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<H323 item=\"1\">"
				+ "<Mode item=\"1\">"
				+ "<Status item=\"1\">{0}</Status>"
				+ "</Mode>"
				+ "</H323>"
				+ "</Status>"
				+ "</XmlDoc>";

			string disabled = string.Format(rX, "Disabled");
			string enabled = string.Format(rX, "Enabled");

			Port.Receive(enabled);
			Port.Receive(disabled);

			Assert.AreEqual(2, responses.Count);
			Assert.IsTrue(responses[0].Data);
			Assert.IsFalse(responses[1].Data);
			Assert.IsFalse(component.H323Enabled);
		}

		[Test]
		public void NoH323RegistrationTest()
		{
			SystemComponent component = new SystemComponent(Codec);

			string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<H323 item=\"1\">"
				+ "<Mode item=\"1\">"
				+ "<Status item=\"1\">{0}</Status>"
				+ "</Mode>"
				+ "</H323>"
				+ "</Status>"
				+ "</XmlDoc>";

			string disabled = string.Format(rX, "Disabled");

			Port.Receive(disabled);

			Assert.AreEqual(false, component.H323Enabled);
		}

		[Test]
		public void NoSipRegistrationTest()
		{
			SystemComponent component = new SystemComponent(Codec);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<SIP item=\"1\">"
				+ "<Registration item=\"1\">"
				+ "<Status item=\"1\">{0}</Status>"
				+ "</Registration>"
				+ "</SIP>"
				+ "</Status>"
				+ "</XmlDoc>";

			string failed = string.Format(rX, eRegState.Failed);

			Port.Receive(failed);

			Assert.AreEqual(eRegState.Failed, component.SipRegistration);
		}
	}
}
