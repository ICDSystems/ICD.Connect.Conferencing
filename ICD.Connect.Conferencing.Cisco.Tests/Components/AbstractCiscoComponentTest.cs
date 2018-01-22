using ICD.Connect.Protocol.Ports;
using ICD.Connect.Protocol.Ports.ComPort;
using NUnit.Framework;

namespace ICD.Connect.Conferencing.Cisco.Tests.Components
{
	public abstract class AbstractCiscoComponentTest
	{
		protected ISerialPort Port { get; private set; }
		protected CiscoCodec Codec { get; private set; }

		[SetUp]
		public virtual void Setup()
		{
			Port = new ComPortPlus(1);
			Codec = new CiscoCodec();

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
