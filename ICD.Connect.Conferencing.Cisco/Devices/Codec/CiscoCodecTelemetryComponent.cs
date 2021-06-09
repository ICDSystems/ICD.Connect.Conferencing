using System;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.Peripherals;
using ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Telemetry.Attributes;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec
{
	internal sealed class CiscoCodecTelemetryComponent
	{
		#region Private Members

		[NotNull] private readonly CiscoCodecDevice m_Codec;
		[NotNull]
		private CiscoCodecDevice Codec { get { return m_Codec; }}

		private float m_AirQualityIndex;
		private bool m_SipIsRegistered;
		private string m_SipLocalName;
		private string m_SipRegistrationStatus;

		#endregion

		#region Events

		[EventTelemetry("OnAirQualityIndexChanged")]
		public event EventHandler<FloatEventArgs> OnAirQualityIndexChanged;

		[EventTelemetry(DialingTelemetryNames.SIP_ENABLED_CHANGED)]
		public event EventHandler<BoolEventArgs> OnSipIsRegisteredChanged;

		[EventTelemetry(DialingTelemetryNames.SIP_LOCAL_NAME_CHANGED)]
		public event EventHandler<StringEventArgs> OnSipLocalNameChanged;

		[EventTelemetry(DialingTelemetryNames.SIP_STATUS_CHANGED)]
		public event EventHandler<StringEventArgs> OnSipRegistrationStatusChanged;

		#endregion

		#region Properties

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

		[PropertyTelemetry(DialingTelemetryNames.SIP_ENABLED, null, DialingTelemetryNames.SIP_ENABLED_CHANGED)]
		public bool SipIsRegistered
		{
			get { return m_SipIsRegistered; }
			private set
			{
				if (m_SipIsRegistered == value)
					return;

				m_SipIsRegistered = value;
				OnSipIsRegisteredChanged.Raise(this, m_SipIsRegistered);
			}
		}

		[PropertyTelemetry(DialingTelemetryNames.SIP_LOCAL_NAME, null, DialingTelemetryNames.SIP_LOCAL_NAME_CHANGED)]
		public string SipLocalName
		{
			get { return m_SipLocalName; }
			private set
			{
				if (m_SipLocalName == value)
					return;

				m_SipLocalName = value;
				OnSipLocalNameChanged.Raise(this, m_SipLocalName);
			}
		}

		[PropertyTelemetry(DialingTelemetryNames.SIP_STATUS, null, DialingTelemetryNames.SIP_STATUS_CHANGED)]
		public string SipRegistrationStatus
		{
			get { return m_SipRegistrationStatus; }
			private set
			{
				if (m_SipRegistrationStatus == value)
					return;

				m_SipRegistrationStatus = value;
				OnSipRegistrationStatusChanged.Raise(this, m_SipRegistrationStatus);
			}
		}

		#endregion

		#region Constructor

		public CiscoCodecTelemetryComponent([NotNull] CiscoCodecDevice codec)
		{
			if (codec == null)
				throw new ArgumentNullException("codec");

			m_Codec = codec;

			Subscribe(m_Codec);

			Update();
		}

		#endregion

		#region Private Methods

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

		private void UpdateSipInformation()
		{
			var systemComponent = Codec.Components.GetComponent<SystemComponent>();
			if (systemComponent == null)
				return;

			// IsRegistered
			IcdHashSet<SipRegistration> registrations = systemComponent.GetSipRegistrations().ToIcdHashSet();
			SipIsRegistered = registrations.Any(r => r.Registration == eRegState.Registered)
			                  && registrations.All(r => r.Registration != eRegState.Failed);

			// LocalName
			SipLocalName = string.Join(", ", systemComponent.GetSipRegistrations()
			                                                .Select(r => r.Uri)
			                                                .ToArray());

			// RegistrationStatus
			SipRegistrationStatus = string.Join(", ", systemComponent.GetSipRegistrations()
			                                                         .Select(r => r.Registration.ToString())
			                                                         .ToArray());
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
				systemComponent.OnSipRegistrationAdded += SystemComponentOnSipRegistrationAdded;
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

		private void SystemComponentOnSipRegistrationAdded(object sender, IntEventArgs e)
		{
			SipRegistration sip = Codec.Components.GetComponent<SystemComponent>().GetSipRegistration(e.Data);
			Subscribe(sip);

			UpdateSipInformation();
		}

		private void PeripheralsComponentOnAirQualityIndexChanged(object sender, FloatEventArgs e)
		{
			AirQualityIndex = e.Data;
		}

		#endregion

		#region Sip Callbacks

		private void Subscribe(SipRegistration sip)
		{
			if (sip == null)
				return;

			sip.OnReasonChange += SipOnReasonChange;
			sip.OnRegistrationChange += SipOnRegistrationChange;
			sip.OnUriChange += SipOnUriChange;
			sip.OnProxyStatusChanged += SipOnProxyStatusChanged;
			sip.OnProxyAddressChanged += SipOnProxyAddressChanged;
		}

		private void SipOnReasonChange(object sender, StringEventArgs e)
		{
			UpdateSipInformation();
		}

		private void SipOnRegistrationChange(object sender, RegistrationEventArgs e)
		{
			UpdateSipInformation();
		}

		private void SipOnUriChange(object sender, StringEventArgs e)
		{
			UpdateSipInformation();
		}

		private void SipOnProxyStatusChanged(object sender, StringEventArgs e)
		{
			UpdateSipInformation();
		}

		private void SipOnProxyAddressChanged(object sender, StringEventArgs e)
		{
			UpdateSipInformation();
		}

		#endregion
	}
}
