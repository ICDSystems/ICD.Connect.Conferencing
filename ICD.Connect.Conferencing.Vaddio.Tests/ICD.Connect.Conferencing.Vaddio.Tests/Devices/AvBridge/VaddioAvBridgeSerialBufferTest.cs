using ICD.Common.Utils;
using ICD.Connect.Conferencing.Vaddio.Devices.AvBridge;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Vaddio.Tests.Devices.AvBridge
{
	[TestFixture]
	public sealed class VaddioAvBridgeSerialBufferTest
	{
		public sealed class VaddioAvBridgeSerialBufferProcessDelimterTestData
		{
			public string Data { get; set; }
			public string Expected { get; set; }
		}

		private static readonly VaddioAvBridgeSerialBufferProcessDelimterTestData[] s_TestData =
		{
			// Standard response.
			new VaddioAvBridgeSerialBufferProcessDelimterTestData
			{
				Data = "audio mute on\r\nOK\r\n>\r\n",
				Expected = "audio mute on\r\nOK\r\n"
			},

			// Response with another delimiter ':' in the middle of the response.
			new VaddioAvBridgeSerialBufferProcessDelimterTestData
			{
				Data = "audio volume get\r\nvolume: 10\r\nOK\r\n>\r\n",
				Expected = "audio volume get\r\nvolume: 10\r\nOK\r\n"
			},

			// Error response.
			new VaddioAvBridgeSerialBufferProcessDelimterTestData
			{
				Data = "gibberish\r\nSyntax error: Unknown or incomplete command\r\n>",
				Expected = "gibberish\r\nSyntax error: Unknown or incomplete command\r\n"
			},

			// Login prompt followed by other response data.
			new VaddioAvBridgeSerialBufferProcessDelimterTestData
			{
				Data = "login:\r\nadmin\r\nPassword:\r\n\r\n>audio mute on\r\nOK\r\n>\r\n",
				Expected = "audio mute on\r\nOK\r\n"
			}
		};


		[Test]
		public void ProcessDelimterTest(
			[ValueSource(nameof(s_TestData))] VaddioAvBridgeSerialBufferProcessDelimterTestData testData)
		{
			string processed = null;

			VaddioAvBridgeSerialBuffer buffer = new VaddioAvBridgeSerialBuffer();
			buffer.OnCompletedSerial += (sender, args) => processed = args.Data;

			buffer.Enqueue(testData.Data);

			ThreadingUtils.Sleep(3000);

			Assert.AreEqual(testData.Expected, processed);
		}
	}
}
