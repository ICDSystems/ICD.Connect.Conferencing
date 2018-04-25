using System;
using ICD.Connect.Devices;
using ICD.Connect.Settings.Attributes;

namespace ICD.Connect.Conferencing.Mock
{
	[KrangSettings(FACTORY_NAME)]
	public sealed class MockConferencingDeviceSettings : AbstractDeviceSettings, IMockConferencingDeviceSettings
	{
		private const string FACTORY_NAME = "MockConferencingDevice";

		public override string FactoryName { get { return FACTORY_NAME; } }

		public override Type OriginatorType { get { return typeof(MockConferencingDevice); } }
	}
}
