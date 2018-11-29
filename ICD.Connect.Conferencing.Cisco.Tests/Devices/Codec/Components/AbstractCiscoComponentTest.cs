using ICD.Connect.Conferencing.Cisco.Devices.Codec;
using ICD.Connect.Protocol.Mock.Ports.ComPort;
using ICD.Connect.Protocol.Ports;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Devices.Codec.Components
{
	public abstract class AbstractCiscoComponentTest
	{
		protected ISerialPort Port { get; private set; }
		protected CiscoCodecDevice Codec { get; private set; }

		[SetUp]
		public virtual void Setup()
		{
			Port = new MockComPort();
			Codec = new CiscoCodecDevice();

			Codec.SetPort(Port);
			Codec.Connect();
		}

		[TearDown]
		public virtual void TearDown()
		{
			Codec.Disconnect();

			Port = null;
			Codec = null;
		}
	}
}
