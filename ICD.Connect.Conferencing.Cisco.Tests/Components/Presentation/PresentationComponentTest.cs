using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using NUnit.Framework;
using ICD.Connect.Conferencing.Cisco.Components.Presentation;

namespace ICD.Connect.Conferencing.Cisco.Tests.Components.Presentation
{
	[TestFixture]
	public sealed class PresentationComponentTest : AbstractCiscoComponentTest
	{
		[Test]
		public void PresentationViewChangedFeedbackTest()
		{
			PresentationComponent component = new PresentationComponent(Codec);

			List<LayoutEventArgs> responses = new List<LayoutEventArgs>();

			component.OnPresentationViewChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Video item=\"1\">"
				+ "<Layout item=\"1\">"
				+ "<PresentationView item=\"1\">{0}</PresentationView>"
				+ "</Layout>"
				+ "</Video>"
				+ "</Status>"
				+ "</XmlDoc>";

			string maximized = string.Format(rX, eLayout.Maximized);
			string minimized = string.Format(rX, eLayout.Minimized);
			string def = string.Format(rX, eLayout.Default);

			Port.Receive(maximized);
			Port.Receive(minimized);
			Port.Receive(def);

			Assert.AreEqual(3, responses.Count);
			Assert.AreEqual(eLayout.Maximized, responses[0].Data);
			Assert.AreEqual(eLayout.Minimized, responses[1].Data);
			Assert.AreEqual(eLayout.Default, responses[2].Data);
			Assert.AreEqual(eLayout.Default, component.PresentationView);
		}

		[Test]
		public void PresentationStoppedFeedbackTest()
		{
			PresentationComponent component = new PresentationComponent(Codec);

			List<StringEventArgs> responses = new List<StringEventArgs>();

			component.OnPresentationStopped += (sender, e) => responses.Add(e);

			string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Event>"
				+ "<PresentationStopped item=\"1\">"
				+ "<Cause item=\"1\">{0}</Cause>"
				+ "<ConferenceId item=\"1\">0</ConferenceId>"
				+ "<Mode item=\"1\">Sending</Mode>"
				+ "<SiteId item=\"1\">1</SiteId>"
				+ "<LocalInstance item=\"1\">1</LocalInstance>"
				+ "</PresentationStopped>"
				+ "</Event>"
				+ "</XmlDoc>";

			rX = string.Format(rX, "userRequested");

			Port.Receive(rX);

			Assert.AreEqual(1, responses.Count);
			Assert.AreEqual("userRequested", responses[0].Data);
		}
	}
}
