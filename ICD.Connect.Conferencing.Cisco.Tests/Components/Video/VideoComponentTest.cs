using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using NUnit.Framework;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;

namespace ICD.Connect.Conferencing.Cisco.Tests.Components.Video
{
	[TestFixture]
	public sealed class VideoComponentTest : AbstractCiscoComponentTest
	{
		[Test]
		public void SelfViewEnabledFeedbackTest()
		{
			VideoComponent component = new VideoComponent(Codec);

			List<BoolEventArgs> responses = new List<BoolEventArgs>();

			component.OnSelfViewEnabledChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Video item=\"1\">"
				+ "<Selfview item=\"1\">"
				+ "<Mode item=\"1\">{0}</Mode>"
				+ "</Selfview>"
				+ "</Video>"
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
			Assert.IsTrue(component.SelfViewEnabled);
		}

		[Test]
		public void SelfViewFullScreenEnabledFeedbackTest()
		{
			VideoComponent component = new VideoComponent(Codec);

			List<BoolEventArgs> responses = new List<BoolEventArgs>();

			component.OnSelfViewFullScreenEnabledChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Video item=\"1\">"
				+ "<Selfview item=\"1\">"
				+ "<FullscreenMode item=\"1\">{0}</FullscreenMode>"
				+ "</Selfview>"
				+ "</Video>"
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
			Assert.IsTrue(component.SelfViewFullScreenEnabled);
		}

		[Test]
		public void SelfViewPositionChangedFeedbackTest()
		{
			VideoComponent component = new VideoComponent(Codec);

			List<PipPositionEventArgs> responses = new List<PipPositionEventArgs>();

			component.OnSelfViewPositionChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Video item=\"1\">"
				+ "<Selfview item=\"1\">"
				+ "<PIPPosition item=\"1\">{0}</PIPPosition>"
				+ "</Selfview>"
				+ "</Video>"
				+ "</Status>"
				+ "</XmlDoc>";

			string upperLeft = string.Format(rX, ePipPosition.UpperLeft);
			string upperRight = string.Format(rX, ePipPosition.UpperRight);
			string centerLeft = string.Format(rX, ePipPosition.CenterLeft);

			Port.Receive(upperLeft);
			Port.Receive(upperRight);
			Port.Receive(centerLeft);

			Assert.AreEqual(3, responses.Count);
			Assert.AreEqual(ePipPosition.UpperLeft, responses[0].Data);
			Assert.AreEqual(ePipPosition.UpperRight, responses[1].Data);
			Assert.AreEqual(ePipPosition.CenterLeft, responses[2].Data);
			Assert.AreEqual(ePipPosition.CenterLeft, component.SelfViewPosition);
		}

		[Test]
		public void SelfViewMonitorChangedFeedbackTest()
		{
			VideoComponent component = new VideoComponent(Codec);

			List<SelfViewMonitorRoleEventArgs> responses = new List<SelfViewMonitorRoleEventArgs>();

			component.OnSelfViewMonitorChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Video item=\"1\">"
				+ "<Selfview item=\"1\">"
				+ "<OnMonitorRole item=\"1\">{0}</OnMonitorRole>"
				+ "</Selfview>"
				+ "</Video>"
				+ "</Status>"
				+ "</XmlDoc>";

			string first = string.Format(rX, eSelfViewMonitorRole.First);
			string second = string.Format(rX, eSelfViewMonitorRole.Second);
			string third = string.Format(rX, eSelfViewMonitorRole.Third);

			Port.Receive(first);
			Port.Receive(second);
			Port.Receive(third);

			Assert.AreEqual(3, responses.Count);
			Assert.AreEqual(eSelfViewMonitorRole.First, responses[0].Data);
			Assert.AreEqual(eSelfViewMonitorRole.Second, responses[1].Data);
			Assert.AreEqual(eSelfViewMonitorRole.Third, responses[2].Data);
			Assert.AreEqual(eSelfViewMonitorRole.Third, component.SelfViewMonitor);
		}

		[Test]
		public void ActiveSpeakerPositionChangedFeedbackTest()
		{
			VideoComponent component = new VideoComponent(Codec);

			List<PipPositionEventArgs> responses = new List<PipPositionEventArgs>();

			component.OnActiveSpeakerPositionChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Video>"
				+ "<ActiveSpeaker>"
				+ "<PIPPosition>{0}</PIPPosition>"
				+ "</ActiveSpeaker>"
				+ "</Video>"
				+ "</Status>"
				+ "</XmlDoc>";

			string upperLeft = string.Format(rX, ePipPosition.UpperLeft);
			string upperRight = string.Format(rX, ePipPosition.UpperRight);
			string centerLeft = string.Format(rX, ePipPosition.CenterLeft);

			Port.Receive(upperLeft);
			Port.Receive(upperRight);
			Port.Receive(centerLeft);

			Assert.AreEqual(3, responses.Count);
			Assert.AreEqual(ePipPosition.UpperLeft, responses[0].Data);
			Assert.AreEqual(ePipPosition.UpperRight, responses[1].Data);
			Assert.AreEqual(ePipPosition.CenterLeft, responses[2].Data);
			Assert.AreEqual(ePipPosition.CenterLeft, component.ActiveSpeakerPosition);
		}

		[Test]
		public void MainVideoSourceChangedFeedbackTest()
		{
			VideoComponent component = new VideoComponent(Codec);

			List<IntEventArgs> responses = new List<IntEventArgs>();

			component.OnMainVideoSourceChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Video item=\"1\">"
				+ "<Input item=\"1\">"
				+ "<MainVideoSource item=\"1\">{0}</MainVideoSource>"
				+ "</Input>"
				+ "</Video>"
				+ "</Status>"
				+ "</XmlDoc>";

			string one = string.Format(rX, 1);
			string two = string.Format(rX, 2);
			string three = string.Format(rX, 3);

			Port.Receive(one);
			Port.Receive(two);
			Port.Receive(three);

			Assert.AreEqual(3, responses.Count);
			Assert.AreEqual(1, responses[0].Data);
			Assert.AreEqual(2, responses[1].Data);
			Assert.AreEqual(3, responses[2].Data);
			Assert.AreEqual(3, component.MainVideoSource);
		}

		[Test]
		public void ActiveVideoConnectorTest()
		{
			VideoComponent component = new VideoComponent(Codec);

			string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Video item=\"1\">"
				+ "<Input item=\"1\">"
				+ "<MainVideoSource item=\"1\">{0}</MainVideoSource>"
				+ "<Source item=\"{0}\">"
				+ "<ConnectorId item=\"1\">{1}</ConnectorId>"
				+ "<MediaChannelId item=\"1\">109</MediaChannelId>"
				+ "<Resolution item=\"1\">"
				+ "<FormatStatus item=\"1\">Ok</FormatStatus>"
				+ "<FormatType item=\"1\">Digital</FormatType>"
				+ "<Height item=\"1\">1080</Height>"
				+ "<RefreshRate item=\"1\">60</RefreshRate>"
				+ "<Width item=\"1\">1920</Width>"
				+ "</Resolution>"
				+ "</Source>"
				+ "</Input>"
				+ "</Video>"
				+ "</Status>"
				+ "</XmlDoc>";

			rX = string.Format(rX, 1, 3);

			Port.Receive(rX);

			Assert.AreEqual(1, component.MainVideoSource);
		}
	}
}
