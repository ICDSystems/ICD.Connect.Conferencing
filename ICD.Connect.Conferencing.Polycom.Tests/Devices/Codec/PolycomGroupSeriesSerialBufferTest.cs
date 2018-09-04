using System.Collections.Generic;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Polycom.Devices.Codec;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Polycom.Tests.Devices.Codec
{
	[TestFixture]
	public sealed class PolycomGroupSeriesSerialBufferTest
	{
		[Test]
		public void EnqueueConcatenatedLineTest()
		{
			const string data =
				"calendarmeetings info start\r\r\nid|AAAlAGNvbmZyb29tQHByb2ZvdW5kdGVjaC5vbm1pY3Jvc29mdC5jb20BUQAICNYR+VuNAABGAAAAAKDEA4kOv71Is4nKeHnLHVUHAKtBwbBNzutGjFgfJdA+tJAAAAAAAQ0AAKtBwbBNzutGjFgfJdA+tJAAAFcf13YAABA=\r\r\n";

			List<StringEventArgs> eventArgs = new List<StringEventArgs>();

			PolycomGroupSeriesSerialBuffer buffer = new PolycomGroupSeriesSerialBuffer();
			buffer.OnCompletedSerial += (sender, args) => eventArgs.Add(args);

			buffer.Enqueue(data);

			Assert.AreEqual(2, eventArgs.Count);
		}
	}
}
