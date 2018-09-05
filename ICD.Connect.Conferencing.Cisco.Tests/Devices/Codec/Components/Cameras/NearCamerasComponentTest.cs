using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Cameras;

namespace ICD.Connect.Conferencing.Cisco.Tests.Components.Cameras
{
	public sealed class NearCamerasComponentTest : AbstractCiscoComponentTest
	{
		[Test]
		public void PresetListTest()
		{
			NearCamerasComponent component = new NearCamerasComponent(Codec);

			int responses = 0;

			component.OnPresetsChanged += (sender, e) => responses++;

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<PresetListResult status=\"OK\">"
				+ "<Preset item=\"1\" maxOccurrence=\"n\">"
				+ "<CameraId>1</CameraId>"
				+ "<Name></Name>"
				+ "<PresetId>1</PresetId>"
				+ "</Preset>"
				+ "<Preset item=\"2\" maxOccurrence=\"n\">"
				+ "<CameraId>1</CameraId>"
				+ "<Name></Name>"
				+ "<PresetId>2</PresetId>"
				+ "</Preset>"
				+ "<Preset item=\"3\" maxOccurrence=\"n\">"
				+ "<CameraId>1</CameraId>"
				+ "<Name></Name>"
				+ "<PresetId>3</PresetId>"
				+ "</Preset>"
				+ "<Preset item=\"4\" maxOccurrence=\"n\">"
				+ "<CameraId>1</CameraId>"
				+ "<Name></Name>"
				+ "<PresetId>10</PresetId>"
				+ "</Preset>"
				+ "</PresetListResult>"
				+ "</XmlDoc>";

			Port.Receive(rX);

			Assert.AreEqual(1, responses);
			Assert.AreEqual(4, component.GetCameraPresets().Count());
		}

		[Test]
		public void CamerasChangedFeedbackTest()
		{
			NearCamerasComponent component = new NearCamerasComponent(Codec);

			List<EventArgs> responses = new List<EventArgs>();

			component.OnCamerasChanged += (sender, e) => responses.Add(e);

			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Cameras>"
				+ "<Camera item=\"1\" maxOccurrence=\"n\">"
				+ "<Capabilities>"
				+ "<Options>ptzf</Options>"
				+ "</Capabilities>"
				+ "<Connected>True</Connected>"
				+ "<Flip>Off</Flip>"
				+ "<Manufacturer>Cisco</Manufacturer>"
				+ "<Model>SX10</Model>"
				+ "</Camera>"
				+ "</Cameras>"
				+ "</Status>"
				+ "</XmlDoc>";

			Port.Receive(rX);

			Assert.AreEqual(1, responses.Count);
			Assert.AreEqual(1, component.CamerasCount);
			Assert.NotNull(component.GetCamera(1));
		}

		[Test]
		public void NoCamerasTest()
		{
			NearCamerasComponent component = new NearCamerasComponent(Codec);

			List<EventArgs> responses = new List<EventArgs>();

			component.OnCamerasChanged += (sender, e) => responses.Add(e);

			// Not sure what the correct empty format is
			const string rX =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Cameras>"
				+ "</Cameras>"
				+ "</Status>"
				+ "</XmlDoc>";

			const string rX2 =
				"<XmlDoc resultId=\"\">"
				+ "<Status>"
				+ "<Cameras />"
				+ "</Status>"
				+ "</XmlDoc>";

			Port.Receive(rX);
			Port.Receive(rX2);

			Assert.AreEqual(0, component.CamerasCount);
		}
	}
}