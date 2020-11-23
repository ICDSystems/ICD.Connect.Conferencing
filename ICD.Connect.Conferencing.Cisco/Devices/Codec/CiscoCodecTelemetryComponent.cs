using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec
{
	internal sealed class CiscoCodecTelemetryComponent
	{
		[NotNull] private readonly CiscoCodecDevice m_Codec;

		[NotNull]
		private CiscoCodecDevice Codec { get { return m_Codec; }}

		public CiscoCodecTelemetryComponent([NotNull] CiscoCodecDevice codec)
		{
			if (codec == null)
				throw new ArgumentNullException("codec");

			m_Codec = codec;

			Subscribe(m_Codec);

			Update();
		}

		#region Methods

		private void Update()
		{
			SystemComponent systemComponent = Codec.Components.GetComponent<SystemComponent>();
			if (systemComponent == null)
				return;

			Codec.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4Address = systemComponent.Address;
			Codec.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4SubnetMask = systemComponent.SubnetMask;
			Codec.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4Gateway = systemComponent.Gateway;
			Codec.MonitoredDeviceInfo.FirmwareVersion = systemComponent.SoftwareVersion;
			Codec.MonitoredDeviceInfo.FirmwareDate = DateTime.Parse(systemComponent.SoftwareVersionDate);
			Codec.MonitoredDeviceInfo.Model = systemComponent.Platform;
			Codec.MonitoredDeviceInfo.SerialNumber = systemComponent.SerialNumber;
		}

		#endregion

		#region Provider Callbacks

		private void Subscribe(CiscoCodecDevice codec)
		{
			if (codec == null)
				return;

			SystemComponent systemComponent = codec.Components.GetComponent<SystemComponent>();
			if (systemComponent == null)
				return;

			systemComponent.OnAddressChanged += SystemComponentOnAddressChanged;
			systemComponent.OnSubnetMaskChanged += SystemComponentOnSubnetMaskChanged;
			systemComponent.OnGatewayChanged += SystemComponentOnGatewayChanged;
			systemComponent.OnSoftwareVersionChanged += SystemComponentOnSoftwareVersionChanged;
			systemComponent.OnSoftwareVerisonDateChanged += SystemComponentOnSoftwareVerisonDateChanged;
			systemComponent.OnPlatformChanged += SystemComponentOnPlatformChanged;
			systemComponent.OnSerialNumberChanged += SystemComponentOnSerialNumberChanged;
		}

		private void SystemComponentOnAddressChanged(object sender, StringEventArgs e)
		{
			Codec.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4Address = e.Data;
		}

		private void SystemComponentOnSubnetMaskChanged(object sender, StringEventArgs e)
		{
			Codec.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4SubnetMask = e.Data;
		}

		private void SystemComponentOnGatewayChanged(object sender, StringEventArgs e)
		{
			Codec.MonitoredDeviceInfo.NetworkInfo.GetOrAddAdapter(1).Ipv4Gateway = e.Data;
		}

		private void SystemComponentOnSoftwareVersionChanged(object sender, StringEventArgs e)
		{
			Codec.MonitoredDeviceInfo.FirmwareVersion = e.Data;
		}

		private void SystemComponentOnSoftwareVerisonDateChanged(object sender, StringEventArgs e)
		{
			Codec.MonitoredDeviceInfo.FirmwareDate = DateTime.Parse(e.Data);
		}

		private void SystemComponentOnPlatformChanged(object sender, StringEventArgs e)
		{
			Codec.MonitoredDeviceInfo.Model = e.Data;
		}

		private void SystemComponentOnSerialNumberChanged(object sender, StringEventArgs e)
		{
			Codec.MonitoredDeviceInfo.SerialNumber = e.Data;
		}

		#endregion
	}
}
