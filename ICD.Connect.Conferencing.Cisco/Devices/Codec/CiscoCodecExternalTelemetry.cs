using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using ICD.Connect.Devices.Telemetry;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Providers.External;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec
{
	public sealed class CiscoCodecExternalTelemetry : AbstractExternalTelemetryProvider<CiscoCodecDevice>
	{
		#region Events

		[EventTelemetry(DeviceTelemetryNames.DEVICE_IP_ADDRESS_CHANGED)]
		public event EventHandler<StringEventArgs> OnAddressChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_IP_SUBNET_CHANGED)]
		public event EventHandler<StringEventArgs> OnSubnetMaskChanged;
		
		[EventTelemetry(DeviceTelemetryNames.DEVICE_IP_GATEWAY_CHANGED)]
		public event EventHandler<StringEventArgs> OnGatewayChanged;
		
		[EventTelemetry(DeviceTelemetryNames.DEVICE_FIRMWARE_VERSION_CHANGED)]
		public event EventHandler<StringEventArgs> OnSoftwareVersionChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_FIRMWARE_DATE_CHANGED)]
		public event EventHandler<StringEventArgs> OnSoftwareVersionDateChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_MODEL_CHANGED)]
		public event EventHandler<StringEventArgs> OnProductPlatformChanged;

		[EventTelemetry(DeviceTelemetryNames.DEVICE_SERIAL_NUMBER_CHANGED)]
		public event EventHandler<StringEventArgs> OnSerialNumberChanged;

		#endregion

		#region Fields

		private string m_Address;
		private string m_SubnetMask;
		private string m_Gateway;
		private string m_SoftwareVersion;
		private string m_SoftwareVersionDate;
		private string m_ProductPlatform;
		private string m_SerialNumber;

		#endregion

		#region Properties

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_IP_ADDRESS, null, DeviceTelemetryNames.DEVICE_IP_ADDRESS_CHANGED)]
		public string Address
		{
			get { return m_Address; }
			private set
			{
				if (m_Address == value)
					return;

				m_Address = value;

				OnAddressChanged.Raise(this, new StringEventArgs(value));
			}
		}

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_IP_SUBNET, null, DeviceTelemetryNames.DEVICE_IP_SUBNET_CHANGED)]
		public string SubnetMask
		{
			get
			{
				return m_SubnetMask;
			}
			private set
			{
				if (m_SubnetMask == value)
					return;

				m_SubnetMask = value;

				OnSubnetMaskChanged.Raise(this, new StringEventArgs(value));
			}
		}

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_IP_GATEWAY, null, DeviceTelemetryNames.DEVICE_IP_GATEWAY_CHANGED)]
		public string Gateway
		{
			get
			{
				return m_Gateway;
			}
			private set
			{
				if (m_Gateway == value)
					return;

				m_Gateway = value;

				OnGatewayChanged.Raise(this, new StringEventArgs(value));
			}
		}

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_FIRMWARE_VERSION, null, DeviceTelemetryNames.DEVICE_FIRMWARE_VERSION_CHANGED)]
		public string SoftwareVersion
		{
			get
			{
				return m_SoftwareVersion;
			}
			private set
			{
				if (m_SoftwareVersion == value)
					return;

				m_SoftwareVersion = value;

				OnSoftwareVersionChanged.Raise(this, new StringEventArgs(value));
			}
		}

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_FIRMWARE_DATE, null, DeviceTelemetryNames.DEVICE_FIRMWARE_DATE_CHANGED)]
		public string SoftwareVersionDate
		{
			get { return m_SoftwareVersionDate; }
			private set
			{
				if (m_SoftwareVersionDate == value)
					return;

				m_SoftwareVersionDate = value;

				OnSoftwareVersionDateChanged.Raise(this, new StringEventArgs(value));
			}
		}

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_MODEL, null, DeviceTelemetryNames.DEVICE_MODEL_CHANGED)]
		public string ProductPlatform
		{
			get { return m_ProductPlatform; }
			private set
			{
				if (m_ProductPlatform == value)
					return;

				m_ProductPlatform = value;

				OnProductPlatformChanged.Raise(this, new StringEventArgs(value));
			}
		}

		[PropertyTelemetry(DeviceTelemetryNames.DEVICE_SERIAL_NUMBER, null, DeviceTelemetryNames.DEVICE_SERIAL_NUMBER_CHANGED)]
		public string SerialNumber
		{
			get { return m_SerialNumber; }
			private set
			{
				if (m_SerialNumber == value)
					return;

				m_SerialNumber = value;

				OnSerialNumberChanged.Raise(this, new StringEventArgs(value));
			}
		}

		#endregion

		#region Methods

		protected override void SetParent(CiscoCodecDevice provider)
		{
			base.SetParent(provider);

			Update();
		}

		private void Update()
		{
			SystemComponent systemComponent = Parent == null ? null : Parent.Components.GetComponent<SystemComponent>();
			if (systemComponent == null)
				return;

			Address = systemComponent.Address;
			SubnetMask = systemComponent.SubnetMask;
			Gateway = systemComponent.Gateway;
			SoftwareVersion = systemComponent.SoftwareVersion;
			SoftwareVersionDate = systemComponent.SoftwareVersionDate;
			ProductPlatform = systemComponent.Platform;
			SerialNumber = systemComponent.SerialNumber;
		}

		#endregion

		#region Provider Callbacks

		protected override void Subscribe(CiscoCodecDevice parent)
		{
			base.Subscribe(parent);

			if (parent == null)
				return;

			SystemComponent systemComponent = parent.Components.GetComponent<SystemComponent>();
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
		}

		protected override void Unsubscribe(CiscoCodecDevice parent)
		{
			base.Unsubscribe(parent);

			if (parent == null)
				return;

			SystemComponent systemComponent = parent.Components.GetComponent<SystemComponent>();
			if (systemComponent != null)
			{
				systemComponent.OnAddressChanged -= SystemComponentOnAddressChanged;
				systemComponent.OnSubnetMaskChanged -= SystemComponentOnSubnetMaskChanged;
				systemComponent.OnGatewayChanged -= SystemComponentOnGatewayChanged;
				systemComponent.OnSoftwareVersionChanged -= SystemComponentOnSoftwareVersionChanged;
				systemComponent.OnSoftwareVerisonDateChanged -= SystemComponentOnSoftwareVerisonDateChanged;
				systemComponent.OnPlatformChanged -= SystemComponentOnPlatformChanged;
				systemComponent.OnSerialNumberChanged -= SystemComponentOnSerialNumberChanged;
			}
		}

		private void SystemComponentOnAddressChanged(object sender, StringEventArgs e)
		{
			Address = e.Data;
		}

		private void SystemComponentOnSubnetMaskChanged(object sender, StringEventArgs e)
		{
			SubnetMask = e.Data;
		}

		private void SystemComponentOnGatewayChanged(object sender, StringEventArgs e)
		{
			Gateway = e.Data;
		}

		private void SystemComponentOnSoftwareVersionChanged(object sender, StringEventArgs e)
		{
			SoftwareVersion = e.Data;
		}

		private void SystemComponentOnSoftwareVerisonDateChanged(object sender, StringEventArgs e)
		{
			SoftwareVersionDate = e.Data;
		}

		private void SystemComponentOnPlatformChanged(object sender, StringEventArgs e)
		{
			ProductPlatform = e.Data;
		}

		private void SystemComponentOnSerialNumberChanged(object sender, StringEventArgs e)
		{
			SerialNumber = e.Data;
		}

		#endregion
	}
}
