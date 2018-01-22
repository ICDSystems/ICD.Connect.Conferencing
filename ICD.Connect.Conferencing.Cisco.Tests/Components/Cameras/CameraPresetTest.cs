using NUnit.Framework;
using ICD.Connect.Conferencing.Cisco.Components.Cameras;

namespace ICD.Connect.Conferencing.Cisco.Tests.Components.Cameras
{
	[TestFixture]
	public sealed class CameraPresetTest
	{
		[Test]
		public void FromXmlTest()
		{
			string xml =
				"<Preset item=\"{0}\" maxOccurrence=\"n\">"
				+ "<CameraId>{1}</CameraId>"
				+ "<Name>{2}</Name>"
				+ "<PresetId>{3}</PresetId>"
				+ "</Preset>";

			xml = string.Format(xml, 3, 5, "Test", 10);

			CameraPreset preset = CameraPreset.FromXml(xml);

			Assert.AreEqual(3, preset.ListPosition);
			Assert.AreEqual(5, preset.CameraId);
			Assert.AreEqual("Test", preset.Name);
			Assert.AreEqual(10, preset.PresetId);
		}
	}
}