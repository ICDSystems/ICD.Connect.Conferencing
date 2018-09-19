using System;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System
{
	/// <summary>
	/// StateComponent provides feedback for the current state of the codec.
	/// </summary>
	public sealed class SystemComponent : AbstractCiscoComponent
	{
		/// <summary>
		/// Raised when the SIP registration status changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<RegistrationEventArgs> OnSipRegistrationChange;

		/// <summary>
		/// Raised when the SIP URI changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSipUriChange;

		/// <summary>
		/// Raised when the SIP proxy address changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSipProxyAddressChanged;

		/// <summary>
		/// Raised when the SIP proxy status changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSipProxyStatusChanged;

		/// <summary>
		/// Raised when the awake status changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnAwakeStateChanged;

		/// <summary>
		/// Raised when the platform changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnPlatformChanged;

		/// <summary>
		/// Raised when the name changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnNameChanged;

		/// <summary>
		/// Raised when the address changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnAddressChanged;

		/// <summary>
		/// Raised when the gateway changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnGatewayChanged;

		/// <summary>
		/// Raised when the subnet mask changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSubnetMaskChanged;

		/// <summary>
		/// Raised when the software version changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSoftwareVersionChanged;

		/// <summary>
		/// Raised when the H323 enabled state changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<BoolEventArgs> OnH323EnabledStateChanged;

		/// <summary>
		/// Raised when the gatekeeper status changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<GatekeeperStatusArgs> OnGatekeeperStatusChanged;

		/// <summary>
		/// Raised when the gatekeeper address changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnGatekeeperAddressChanged;

		private bool m_H323Enabled;
		private eH323GatekeeperStatus m_H323GatekeeperStatus = eH323GatekeeperStatus.Inactive;
		private string m_H323GatekeeperAddress;
		private bool m_Awake;
		private string m_Name;
		private string m_Address;
		private string m_SoftwareVersion;
		private string m_Platform;
		private eRegState m_SipRegistration = eRegState.Unknown;
		private string m_SipUri;
		private string m_SipProxyAddress;
		private string m_SipProxyStatus;
		private string m_Gateway;
		private string m_SubnetMask;

		#region Properties

		/// <summary>
		/// Gets the codec platform.
		/// </summary>
		[PublicAPI]
		public string Platform
		{
			get { return m_Platform; }
			private set
			{
				if (value == m_Platform)
					return;

				m_Platform = value;

				Codec.Log(eSeverity.Informational, "Codec platform is {0}", m_Platform);
				OnPlatformChanged.Raise(this, new StringEventArgs(m_Platform));
			}
		}

		/// <summary>
		/// Gets the name of the codec.
		/// </summary>
		[PublicAPI]
		public string Name
		{
			get { return m_Name; }
			private set
			{
				if (value == m_Name)
					return;

				m_Name = value;

				Codec.Log(eSeverity.Informational, "Codec name is {0}", m_Name);
				OnNameChanged.Raise(this, new StringEventArgs(m_Name));
			}
		}

		/// <summary>
		/// Gets the address of the codec.
		/// </summary>
		[PublicAPI]
		public string Address
		{
			get { return m_Address; }
			private set
			{
				if (value == m_Address)
					return;

				m_Address = value;

				Codec.Log(eSeverity.Informational, "Codec address is {0}", m_Address);
				OnAddressChanged.Raise(this, new StringEventArgs(m_Address));
			}
		}

		/// <summary>
		/// Gets the gateway of the codec.
		/// </summary>
		[PublicAPI]
		public string Gateway
		{
			get { return m_Gateway; }
			private set
			{
				if (value == m_Gateway)
					return;

				m_Gateway = value;

				Codec.Log(eSeverity.Informational, "Codec gateway is {0}", m_Gateway);
				OnGatewayChanged.Raise(this, new StringEventArgs(m_Gateway));
			}
		}

		/// <summary>
		/// Gets the subnet mask of the codec.
		/// </summary>
		[PublicAPI]
		public string SubnetMask
		{
			get { return m_SubnetMask; }
			private set
			{
				if (value == m_SubnetMask)
					return;

				m_SubnetMask = value;

				Codec.Log(eSeverity.Informational, "Codec subnet mask is {0}", m_SubnetMask);
				OnSubnetMaskChanged.Raise(this, new StringEventArgs(m_SubnetMask));
			}
		}

		/// <summary>
		/// Gets the awake status of the codec.
		/// </summary>
		[PublicAPI]
		public bool Awake
		{
			get { return m_Awake; }
			private set
			{
				if (value == m_Awake)
					return;

				m_Awake = value;

				Codec.Log(eSeverity.Informational, m_Awake ? "VTC is Active" : "VTC is in Standby Mode");
				OnAwakeStateChanged.Raise(this, new BoolEventArgs(m_Awake));
			}
		}

		/// <summary>
		/// Gets the current software version.
		/// </summary>
		[PublicAPI]
		public string SoftwareVersion
		{
			get { return m_SoftwareVersion; }
			private set
			{
				if (value == m_SoftwareVersion)
					return;

				m_SoftwareVersion = value;

				Codec.Log(eSeverity.Informational, "Codec software version is {0}", m_SoftwareVersion);
				OnSoftwareVersionChanged.Raise(this, new StringEventArgs(m_SoftwareVersion));
			}
		}

		/// <summary>
		/// Gets the H323 enabled state.
		/// </summary>
		[PublicAPI]
		public bool H323Enabled
		{
			get { return m_H323Enabled; }
			private set
			{
				if (value == m_H323Enabled)
					return;

				m_H323Enabled = value;

				Codec.Log(eSeverity.Informational, "H323 is {0}", m_H323Enabled ? "On" : "Off");
				OnH323EnabledStateChanged.Raise(this, new BoolEventArgs(m_H323Enabled));
			}
		}

		/// <summary>
		/// Gets the Gatekeeper status.
		/// </summary>
		[PublicAPI]
		public eH323GatekeeperStatus H323GatekeeperStatus
		{
			get { return m_H323GatekeeperStatus; }
			private set
			{
				if (value == m_H323GatekeeperStatus)
					return;

				m_H323GatekeeperStatus = value;

				Codec.Log(eSeverity.Informational, "Gatekeeper status is {0}", m_H323GatekeeperStatus);
				OnGatekeeperStatusChanged.Raise(this, new GatekeeperStatusArgs(m_H323GatekeeperStatus));
			}
		}

		/// <summary>
		/// Gets the gatekeeper address.
		/// </summary>
		[PublicAPI]
		public string H323GatekeeperAddress
		{
			get { return m_H323GatekeeperAddress; }
			private set
			{
				if (value == m_H323GatekeeperAddress)
					return;

				m_H323GatekeeperAddress = value;

				Codec.Log(eSeverity.Informational, "Gatekeeper address is {0}", m_H323GatekeeperAddress);
				OnGatekeeperAddressChanged.Raise(this, new StringEventArgs(m_H323GatekeeperAddress));
			}
		}

		/// <summary>
		/// Registration Status
		/// </summary>
		[PublicAPI]
		public eRegState SipRegistration
		{
			get { return m_SipRegistration; }
			private set
			{
				if (value == m_SipRegistration)
					return;

				m_SipRegistration = value;

				Codec.Log(eSeverity.Informational, "SIP Registration status is {0}", m_SipRegistration);
				OnSipRegistrationChange.Raise(this, new RegistrationEventArgs(m_SipRegistration));
			}
		}

		/// <summary>
		/// Gets the SIP URI.
		/// </summary>
		[PublicAPI]
		public string SipUri
		{
			get { return m_SipUri; }
			private set
			{
				if (value == m_SipUri)
					return;

				m_SipUri = value;

				Codec.Log(eSeverity.Informational, "Registered SIP URI is {0}", m_SipUri);
				OnSipUriChange.Raise(this, new StringEventArgs(m_SipUri));
			}
		}

		/// <summary>
		/// Gets the SIP proxy address.
		/// </summary>
		[PublicAPI]
		public string SipProxyAddress
		{
			get { return m_SipProxyAddress; }
			private set
			{
				if (value == m_SipProxyAddress)
					return;

				m_SipProxyAddress = value;

				Codec.Log(eSeverity.Informational, "Registered SIP proxy address is {0}", m_SipProxyAddress);
				OnSipProxyAddressChanged.Raise(this, new StringEventArgs(m_SipProxyAddress));
			}
		}

		/// <summary>
		/// Gets the Sip proxy status.
		/// </summary>
		[PublicAPI]
		public string SipProxyStatus
		{
			get { return m_SipProxyStatus; }
			private set
			{
				if (value == m_SipProxyStatus)
					return;

				m_SipProxyStatus = value;

				Codec.Log(eSeverity.Informational, "Registered SIP proxy status is {0}", m_SipProxyStatus);
				OnSipProxyStatusChanged.Raise(this, new StringEventArgs(m_SipProxyStatus));
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public SystemComponent(CiscoCodecDevice codec) : base(codec)
		{
			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Puts the codec into standby mode.
		/// </summary>
		[PublicAPI]
		public void Standby()
		{
			Codec.SendCommand("xCommand Standby Activate");
			Codec.Log(eSeverity.Informational, "Putting VTC into Standby");
		}

		/// <summary>
		/// Wakes the codec from standby mode.
		/// </summary>
		[PublicAPI]
		public void Wake()
		{
			Codec.SendCommand("xCommand Standby Deactivate");
			Codec.Log(eSeverity.Informational, "Activating VTC");
		}

		[PublicAPI]
		public void ResetSleepTimer(int minutes)
		{
			Codec.SendCommand("xCommand Standby ResetTimer Delay:{0}", minutes);
			Codec.Log(eSeverity.Informational, "Resetting standby timer to {0} minutes", minutes);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Subscribes to the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Subscribe(CiscoCodecDevice codec)
		{
			base.Subscribe(codec);

			if (codec == null)
				return;

			codec.RegisterParserCallback(ParseStandbyStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Standby", "State");
			codec.RegisterParserCallback(ParseSipRegistrationStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Registration", "Status");
			codec.RegisterParserCallback(ParseSipRegistrationUri, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Registration", "URI");
			codec.RegisterParserCallback(ParseSipProxyStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Proxy", "Status");
			codec.RegisterParserCallback(ParseSipProxyAddress, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Proxy", "Address");
			codec.RegisterParserCallback(ParseNameStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "UserInterface", "ContactInfo", "Name");
			codec.RegisterParserCallback(ParseVersionStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "Software", "Version");
			codec.RegisterParserCallback(ParseAddressStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "Address");
			codec.RegisterParserCallback(ParseGatewayStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "Gateway");
			codec.RegisterParserCallback(ParseSubnetMaskStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "SubnetMask");
			codec.RegisterParserCallback(ParseH323EnabledStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Mode", "Status");
			codec.RegisterParserCallback(ParseH323GatekeeperStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Gatekeeper", "Status");
			codec.RegisterParserCallback(ParseH323GatekeeperAddress, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Gatekeeper", "Address");
			codec.RegisterParserCallback(ParsePlatformStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "ProductPlatform");
		}

		/// <summary>
		/// Unsubscribes from the codec events.
		/// </summary>
		/// <param name="codec"></param>
		protected override void Unsubscribe(CiscoCodecDevice codec)
		{
			base.Unsubscribe(codec);

			if (codec == null)
				return;

			codec.UnregisterParserCallback(ParseStandbyStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Standby", "State");
			codec.UnregisterParserCallback(ParseSipRegistrationStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Registration",
			                               "Status");
			codec.UnregisterParserCallback(ParseSipRegistrationUri, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Registration", "URI");
			codec.UnregisterParserCallback(ParseSipProxyStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Proxy", "Status");
			codec.UnregisterParserCallback(ParseSipProxyAddress, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Proxy", "Address");
			codec.UnregisterParserCallback(ParseNameStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "UserInterface", "ContactInfo", "Name");
			codec.UnregisterParserCallback(ParseVersionStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "Software", "Version");
			codec.UnregisterParserCallback(ParseAddressStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "Address");
			codec.UnregisterParserCallback(ParseGatewayStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "Gateway");
			codec.UnregisterParserCallback(ParseSubnetMaskStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "SubnetMask");
			codec.UnregisterParserCallback(ParseH323EnabledStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Mode", "Status");
			codec.UnregisterParserCallback(ParseH323GatekeeperStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Gatekeeper", "Status");
			codec.UnregisterParserCallback(ParseH323GatekeeperAddress, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Gatekeeper",
			                               "Address");
			codec.UnregisterParserCallback(ParsePlatformStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "ProductPlatform");
		}

		private void ParsePlatformStatus(CiscoCodecDevice codec, string resultid, string xml)
		{
			Platform = XmlUtils.GetInnerXml(xml);
		}

		private void ParseStandbyStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			string standby = XmlUtils.GetInnerXml(xml);
			Awake = standby == "Off";
		}

		private void ParseSipRegistrationStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			SipRegistration = EnumUtils.Parse<eRegState>(content, true);
		}

		private void ParseSipRegistrationUri(CiscoCodecDevice codec, string resultid, string xml)
		{
			SipUri = XmlUtils.GetInnerXml(xml);
		}

		private void ParseSipProxyAddress(CiscoCodecDevice codec, string resultid, string xml)
		{
			SipProxyAddress = XmlUtils.GetInnerXml(xml);
		}

		private void ParseSipProxyStatus(CiscoCodecDevice codec, string resultid, string xml)
		{
			SipProxyStatus = XmlUtils.GetInnerXml(xml);
		}

		private void ParseNameStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			Name = XmlUtils.GetInnerXml(xml);
		}

		private void ParseAddressStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			Address = XmlUtils.GetInnerXml(xml);
		}

		private void ParseGatewayStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			Gateway = XmlUtils.GetInnerXml(xml);
		}

		private void ParseSubnetMaskStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			SubnetMask = XmlUtils.GetInnerXml(xml);
		}

		private void ParseVersionStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			SoftwareVersion = XmlUtils.GetInnerXml(xml);
		}

		private void ParseH323EnabledStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			H323Enabled = XmlUtils.GetInnerXml(xml) == "Enabled";
		}

		private void ParseH323GatekeeperStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			string content = XmlUtils.GetInnerXml(xml);
			H323GatekeeperStatus = EnumUtils.Parse<eH323GatekeeperStatus>(content, true);
		}

		private void ParseH323GatekeeperAddress(CiscoCodecDevice codec, string resultid, string xml)
		{
			H323GatekeeperAddress = XmlUtils.GetInnerXml(xml);
		}

		#endregion
	}
}
