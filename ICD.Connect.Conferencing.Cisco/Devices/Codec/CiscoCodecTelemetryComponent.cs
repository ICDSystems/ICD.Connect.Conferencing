using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Peripherals;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec
{
	internal sealed class CiscoCodecTelemetryComponent
	{
		[NotNull] private readonly CiscoCodecDevice m_Codec;
		[NotNull]
		private CiscoCodecDevice Codec { get { return m_Codec; }}

		[EventTelemetry("OnAirQualityIndexChanged")]
		public event EventHandler<FloatEventArgs> OnAirQualityIndexChanged;

		private float m_AirQualityIndex;

		[PropertyTelemetry("AirQualityIndex", null, "OnAirQualityIndexChanged")]
		public float AirQualityIndex
		{
			get { return m_AirQualityIndex; }
			private set
			{
				if (Math.Abs(m_AirQualityIndex - value) < 0.01f)
					return;

				m_AirQualityIndex = value;
				OnAirQualityIndexChanged.Raise(this, m_AirQualityIndex);
			}
		}

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

			Codec.MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).Ipv4Address = systemComponent.Address;
			Codec.MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).Ipv4SubnetMask = systemComponent.SubnetMask;
			Codec.MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).Ipv4Gateway = systemComponent.Gateway;
			Codec.MonitoredDeviceInfo.FirmwareVersion = systemComponent.SoftwareVersion;

			// Try to parse firmware date, and if it doesn't work, set to null
			string firmwareDate = systemComponent.SoftwareVersionDate;
			if (!string.IsNullOrEmpty(firmwareDate))
			{
				try
				{
					Codec.MonitoredDeviceInfo.FirmwareDate = DateTime.Parse(firmwareDate);
				}
				catch (FormatException e)
				{
					Codec.MonitoredDeviceInfo.FirmwareDate = null;
					Codec.Logger.Log(eSeverity.Error, "CiscoCodecTelementry - FormatException parsing SoftwareVersionDate: {0}",
					                 e.Message);
				}
			}
			else
				Codec.MonitoredDeviceInfo.FirmwareDate = null;


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
			if (systemComponent != null)
			{
				systemComponent.OnAddressChanged += SystemComponentOnAddressChanged;
				systemComponent.OnSubnetMaskChanged += SystemComponentOnSubnetMaskChanged;
				systemComponent.OnGatewayChanged += SystemComponentOnGatewayChanged;
				systemComponent.OnSoftwareVersionChanged += SystemComponentOnSoftwareVersionChanged;
				systemComponent.OnSoftwareVerisonDateChanged += SystemComponentOnSoftwareVerisonDateChanged;
				systemComponent.OnPlatformChanged += SystemComponentOnPlatformChanged;
				systemComponent.OnSerialNumberChanged += SystemComponentOnSerialNumberChanged;
			}

			PeripheralsComponent peripheralsComponent = codec.Components.GetComponent<PeripheralsComponent>();
			if (peripheralsComponent == null)
				return;

			peripheralsComponent.OnAirQualityIndexChanged += PeripheralsComponentOnAirQualityIndexChanged;
		}

		private void SystemComponentOnAddressChanged(object sender, StringEventArgs e)
		{
			Codec.MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).Ipv4Address = e.Data;
		}

		private void SystemComponentOnSubnetMaskChanged(object sender, StringEventArgs e)
		{
			Codec.MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).Ipv4SubnetMask = e.Data;
		}

		private void SystemComponentOnGatewayChanged(object sender, StringEventArgs e)
		{
			Codec.MonitoredDeviceInfo.NetworkInfo.Adapters.GetOrAddAdapter(1).Ipv4Gateway = e.Data;
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

		private void PeripheralsComponentOnAirQualityIndexChanged(object sender, FloatEventArgs e)
		{
			AirQualityIndex = e.Data;
		}

		#endregion
	}
}
