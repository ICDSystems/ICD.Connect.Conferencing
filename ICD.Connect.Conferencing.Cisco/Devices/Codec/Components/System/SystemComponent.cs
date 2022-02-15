using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Common.Logging.LoggingContexts;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System
{
	/// <summary>
	/// StateComponent provides feedback for the current state of the codec.
	/// </summary>
	public sealed class SystemComponent : AbstractCiscoComponent
	{

		private const string WEBEX_REGISTRED_STRING = "Registered";

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
		/// Raised when the software version date changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSoftwareVerisonDateChanged;

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

		/// <summary>
		/// Raised when a new SIP registration is discovered.
		/// </summary>
		[PublicAPI]
		public event EventHandler<IntEventArgs> OnSipRegistrationAdded;

		/// <summary>
		/// Raised when the serial number changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnSerialNumberChanged;

		/// <summary>
		/// Raised when the webex registeration status changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnWebexRegistrationStatusChanged;

		private readonly IcdSortedDictionary<int, SipRegistration> m_SipRegistrations;
		private readonly SafeCriticalSection m_SipRegistrationsSection;

		private bool m_H323Enabled;
		private eH323GatekeeperStatus m_H323GatekeeperStatus = eH323GatekeeperStatus.Inactive;
		private string m_H323GatekeeperAddress;
		private bool m_Awake;
		private string m_Name;
		private string m_Address;
		private string m_SoftwareVersion;
		private string m_SoftwareVersionDate;
		private string m_Platform;
		private string m_Gateway;
		private string m_SubnetMask;
		private string m_SerialNumber;
		private bool m_WebexRegistrationStatus;

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

				Codec.Logger.LogSetTo(eSeverity.Informational, "System Platform", m_Platform);

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

				Codec.Logger.LogSetTo(eSeverity.Informational, "System Name", m_Name);

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

				Codec.Logger.LogSetTo(eSeverity.Informational, "System Address", m_Address);

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

				Codec.Logger.LogSetTo(eSeverity.Informational, "System Gateway", m_Gateway);

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

				Codec.Logger.LogSetTo(eSeverity.Informational, "System Subnet Mask", m_SubnetMask);

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

				Codec.Logger.LogSetTo(eSeverity.Informational, "Awake", m_Awake);

				OnAwakeStateChanged.Raise(this, new BoolEventArgs(m_Awake));
			}
		}

		/// <summary>
		/// Converts the software version string provided by the system to the Version type.
		/// </summary>
		[PublicAPI]
		[CanBeNull]
		public Version MilestoneVersion
		{
			get
			{
				try
				{
					// Regex: https://regex101.com/r/7MqECW/1
					const string codecSoftwareVersionRegex = @"(?'prefix'[a-z]*)(?'version'\d+(?:\.\d+)+)(?'suffix'\.[^.]+)";

					// Match against software version string
					Match m = Regex.Match(SoftwareVersion, codecSoftwareVersionRegex);

					// Convert to version data type, this will only preserve the decimal orders of the version
					// Example: version string ce9.9.2.f2110f7eda7 would become 9.9.2
					return new Version(m.Groups["version"].Value);
				}
				catch (Exception e)
				{
					Codec.Logger.Log(eSeverity.Error,
					                 "Error determining Codec milestone version - {0} {1}",
					                 e.Message,
					                 e.StackTrace);
					return null;
				}
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

				Codec.Logger.LogSetTo(eSeverity.Informational, "SoftwareVersion", m_SoftwareVersion);

				OnSoftwareVersionChanged.Raise(this, new StringEventArgs(m_SoftwareVersion));
			}
		}

		/// <summary>
		/// Gets the date of the current software version
		/// </summary>
		public string SoftwareVersionDate
		{
			get
			{
				return m_SoftwareVersionDate;
			}
			private set
			{
				if (m_SoftwareVersionDate == value)
					return;

				m_SoftwareVersionDate = value;

				Codec.Logger.LogSetTo(eSeverity.Informational, "SoftwareVersionDate", m_SoftwareVersionDate);

				OnSoftwareVerisonDateChanged.Raise(this, new StringEventArgs(value));
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

				Codec.Logger.LogSetTo(eSeverity.Informational, "H323Enabled", m_H323Enabled);

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

				Codec.Logger.LogSetTo(eSeverity.Informational, "H323GatekeeperStatus", m_H323GatekeeperStatus);
				
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

				Codec.Logger.LogSetTo(eSeverity.Informational, "H323GatekeeperAddress", m_H323GatekeeperAddress);

				OnGatekeeperAddressChanged.Raise(this, new StringEventArgs(m_H323GatekeeperAddress));
			}
		}

		/// <summary>
		/// Gets the serial number.
		/// </summary>
		[PublicAPI]
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

		public bool WebexRegistraionStatus
		{
			get { return m_WebexRegistrationStatus; }
			private set
			{
				if (m_WebexRegistrationStatus == value)
					return;

				m_WebexRegistrationStatus = value;

				OnWebexRegistrationStatusChanged.Raise(this, value);
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
			m_SipRegistrations = new IcdSortedDictionary<int, SipRegistration>();
			m_SipRegistrationsSection = new SafeCriticalSection();

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
			if (Codec.StandbyToHalfwake)
			{
				Codec.SendCommand("xCommand Standby Halfwake");
				Codec.Logger.Log(eSeverity.Informational, "Putting VTC into Halfwake");
			}
			else
			{
				Codec.SendCommand("xCommand Standby Activate");
				Codec.Logger.Log(eSeverity.Informational, "Putting VTC into Standby");
			}
		}

		/// <summary>
		/// Wakes the codec from standby mode.
		/// </summary>
		[PublicAPI]
		public void Wake()
		{
			Codec.SendCommand("xCommand Standby Deactivate");
			Codec.Logger.Log(eSeverity.Informational, "Activating VTC");
		}

		[PublicAPI]
		public void ResetSleepTimer(int minutes)
		{
			if (Codec.StandbyToHalfwake)
			{
				Codec.SendCommand("xCommand Standby ResetHalfwakeTimer Delay:{0}", minutes);
				Codec.Logger.Log(eSeverity.Informational, "Resetting halfwake timer to {0} minutes", minutes);
			}
			else
			{
				Codec.SendCommand("xCommand Standby ResetTimer Delay:{0}", minutes);
				Codec.Logger.Log(eSeverity.Informational, "Resetting standby timer to {0} minutes", minutes);
			}
		}

		[PublicAPI]
		public void ResetHalfwakeTimer(int minutes)
		{
			Codec.SendCommand("xCommand Standby ResetHalfwakeTimer Delay:{0}", minutes);
			Codec.Logger.Log(eSeverity.Informational, "Resetting halfwake timer to {0} minutes", minutes);
		}

		/// <summary>
		/// Gets the sip registration infos.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<SipRegistration> GetSipRegistrations()
		{
			return m_SipRegistrationsSection.Execute(() => m_SipRegistrations.Values.ToArray(m_SipRegistrations.Count));
		}

		/// <summary>
		/// Gets the sip registration for the given item.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		public SipRegistration GetSipRegistration(int item)
		{
			return m_SipRegistrationsSection.Execute(() => m_SipRegistrations[item]);
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Gets/instantiates the SipRegistration info at the given index.
		/// </summary>
		/// <param name="item"></param>
		/// <returns></returns>
		private SipRegistration LazyLoadSipRegistration(int item)
		{
			SipRegistration registration;

			m_SipRegistrationsSection.Enter();

			try
			{
				if (m_SipRegistrations.TryGetValue(item, out registration))
					return registration;

				registration = new SipRegistration(Codec, item);
				m_SipRegistrations.Add(item, registration);
			}
			finally
			{
				m_SipRegistrationsSection.Leave();
			}

			OnSipRegistrationAdded.Raise(this, new IntEventArgs(item));

			return registration;
		}

		#endregion

		#region Codec Feedback

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
			codec.RegisterParserCallback(ParseSipRegistration, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Registration");
			codec.RegisterParserCallback(ParseSipProxy, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Proxy");
			codec.RegisterParserCallback(ParseNameStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "UserInterface", "ContactInfo", "Name");
			codec.RegisterParserCallback(ParseVersionStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "Software", "Version");
			codec.RegisterParserCallback(ParseVersionDateStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "Software", "ReleaseDate");
			codec.RegisterParserCallback(ParseAddressStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "Address");
			codec.RegisterParserCallback(ParseGatewayStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "Gateway");
			codec.RegisterParserCallback(ParseSubnetMaskStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "SubnetMask");
			codec.RegisterParserCallback(ParseH323EnabledStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Mode", "Status");
			codec.RegisterParserCallback(ParseH323GatekeeperStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Gatekeeper", "Status");
			codec.RegisterParserCallback(ParseH323GatekeeperAddress, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Gatekeeper", "Address");
			codec.RegisterParserCallback(ParsePlatformStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "ProductPlatform");
			codec.RegisterParserCallback(ParseSerialNumber, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "Hardware", "Module", "SerialNumber");
			codec.RegisterParserCallback(ParseWebexRegistrationStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Webex", "Status");
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
			codec.UnregisterParserCallback(ParseSipRegistration, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Registration");
			codec.UnregisterParserCallback(ParseSipProxy, CiscoCodecDevice.XSTATUS_ELEMENT, "SIP", "Proxy");
			codec.UnregisterParserCallback(ParseNameStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "UserInterface", "ContactInfo", "Name");
			codec.UnregisterParserCallback(ParseVersionStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "Software", "Version");
			codec.UnregisterParserCallback(ParseVersionDateStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "Software", "ReleaseDate");
			codec.UnregisterParserCallback(ParseAddressStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "Address");
			codec.UnregisterParserCallback(ParseGatewayStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "Gateway");
			codec.UnregisterParserCallback(ParseSubnetMaskStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Network", "IPv4", "SubnetMask");
			codec.UnregisterParserCallback(ParseH323EnabledStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Mode", "Status");
			codec.UnregisterParserCallback(ParseH323GatekeeperStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Gatekeeper", "Status");
			codec.UnregisterParserCallback(ParseH323GatekeeperAddress, CiscoCodecDevice.XSTATUS_ELEMENT, "H323", "Gatekeeper", "Address");
			codec.UnregisterParserCallback(ParsePlatformStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit", "ProductPlatform");
			codec.UnregisterParserCallback(ParseSerialNumber, CiscoCodecDevice.XSTATUS_ELEMENT, "SystemUnit",
			                               "Hardware", "Module", "SerialNumber");
			codec.UnregisterParserCallback(ParseWebexRegistrationStatus, CiscoCodecDevice.XSTATUS_ELEMENT, "Webex", "Status");
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

		private void ParseSipRegistration(CiscoCodecDevice sender, string resultId, string xml)
		{
			// CE8 doesn't support multiple SIP registrations, item id may be absent?
			string itemString;
			int item = XmlUtils.TryGetAttribute(xml, "item", out itemString) ? int.Parse(itemString) : 1;

			LazyLoadSipRegistration(item).ParseXml(xml);
		}

		private void ParseSipProxy(CiscoCodecDevice codec, string resultid, string xml)
		{
			// CE8 doesn't support multiple SIP registrations, item id may be absent?
			string itemString;
			int item = XmlUtils.TryGetAttribute(xml, "item", out itemString) ? int.Parse(itemString) : 1;

			LazyLoadSipRegistration(item).ParseXml(xml);
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

		private void ParseVersionDateStatus(CiscoCodecDevice sender, string resultId, string xml)
		{
			SoftwareVersionDate = XmlUtils.GetInnerXml(xml);
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

		private void ParseSerialNumber(CiscoCodecDevice codec, string resultid, string xml)
		{
			SerialNumber = XmlUtils.GetInnerXml(xml);
		}

		private void ParseWebexRegistrationStatus(CiscoCodecDevice codec, string resultid, string xml)
		{
			string status = XmlUtils.GetInnerXml(xml);
			WebexRegistraionStatus = String.Equals(status, WEBEX_REGISTRED_STRING, StringComparison.OrdinalIgnoreCase);
		}

		#endregion

		#region Console

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Platform", Platform);
			addRow("Name", Name);
			addRow("Address", Address);
			addRow("Gateway", Gateway);
			addRow("Subnet Mask", SubnetMask);
			addRow("Awake", Awake);
			addRow("Software Version", SoftwareVersion);
			addRow("H323 Enabled", H323Enabled);
			addRow("H323 Gatekeeper Status", H323GatekeeperStatus);
			addRow("H323 Gatekeeper Address", H323GatekeeperAddress);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Standby", "", () => Standby());
			yield return new ConsoleCommand("Wake", "", () => Wake());
			yield return new GenericConsoleCommand<int>("ResetSleepTimer", "ResetSleepTimer <MINUTES>", i => ResetSleepTimer(i));

			yield return new ConsoleCommand("PrintSipRegistrations", "", () => PrintSipRegistrations());
		}

		/// <summary>
		/// Workaround for "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		private string PrintSipRegistrations()
		{
			TableBuilder builder = new TableBuilder("Item", "URI", "Registration", "Reason", "Proxy Address", "Proxy Status");

			foreach (SipRegistration registration in GetSipRegistrations())
			{
				builder.AddRow(registration.Item,
					registration.Uri,
					registration.Registration,
					registration.Reason,
					registration.ProxyAddress,
					registration.ProxyStatus);
			}

			return builder.ToString();
		}

		#endregion
	}
}
