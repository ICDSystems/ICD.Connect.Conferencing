using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Video;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components.Video
{
	[TestFixture]
	public sealed class VideoSourceTest
	{
		[Test]
		public void FromXmlTest()
		{
			string xml =
				"<Source item=\"{0}\">"
				+ "<ConnectorId item=\"1\">{1}</ConnectorId>"
				+ "<MediaChannelId item=\"1\">109</MediaChannelId>"
				+ "<Resolution item=\"1\">"
				+ "<FormatStatus item=\"1\">Ok</FormatStatus>"
				+ "<FormatType item=\"1\">Digital</FormatType>"
				+ "<Height item=\"1\">1080</Height>"
				+ "<RefreshRate item=\"1\">60</RefreshRate>"
				+ "<Width item=\"1\">1920</Width>"
				+ "</Resolution>"
				+ "</Source>";

			xml = string.Format(xml, 2, 3);

			VideoSource source = VideoSource.FromXml(xml);

			Assert.NotNull(source);
			Assert.AreEqual(2, source.SourceId);
			Assert.AreEqual(3, source.ConnectorId);
		}
	}
}
