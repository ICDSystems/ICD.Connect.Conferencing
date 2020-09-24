using System;
using ICD.Common.Properties;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Xml;
using ICD.Common.Logging.LoggingContexts;

namespace ICD.Connect.Conferencing.Cisco.Devices.Codec.Components.System
{
	public sealed class SipRegistration
	{
		/// <summary>
		/// Raised when the SIP registration reason changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnReasonChange;

		/// <summary>
		/// Raised when the SIP registration status changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<RegistrationEventArgs> OnRegistrationChange;

		/// <summary>
		/// Raised when the SIP URI changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnUriChange;

		/// <summary>
		/// Raised when the SIP proxy address changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnProxyAddressChanged;

		/// <summary>
		/// Raised when the SIP proxy status changes.
		/// </summary>
		[PublicAPI]
		public event EventHandler<StringEventArgs> OnProxyStatusChanged;

		private readonly CiscoCodecDevice m_Codec;
		private readonly int m_Item;

		private string m_SipReason;
		private eRegState m_SipRegistration;
		private string m_SipUri;
		private string m_SipProxyAddress;
		private string m_SipProxyStatus;

		#region Properties

		/// <summary>
		/// Gets the item index for this SIP registration.
		/// </summary>
		public int Item { get { return m_Item; } }

		/// <summary>
		/// Registration Reason.
		/// </summary>
		[PublicAPI]
		public string Reason
		{
			get { return m_SipReason; }
			private set
			{
				if (value == m_SipReason)
					return;

				m_SipReason = value;

				m_Codec.Logger.LogSetTo(eSeverity.Informational, "SIP Reason " + m_Item, m_SipReason);

				OnReasonChange.Raise(this, new StringEventArgs(m_SipReason));
			}
		}

		/// <summary>
		/// Registration Status.
		/// </summary>
		[PublicAPI]
		public eRegState Registration
		{
			get { return m_SipRegistration; }
			private set
			{
				if (value == m_SipRegistration)
					return;

				m_SipRegistration = value;

				m_Codec.Logger.LogSetTo(eSeverity.Informational, "SIP Registration " + m_Item, m_SipRegistration);

				OnRegistrationChange.Raise(this, new RegistrationEventArgs(m_SipRegistration));
			}
		}

		/// <summary>
		/// Gets the SIP URI.
		/// </summary>
		[PublicAPI]
		public string Uri
		{
			get { return m_SipUri; }
			private set
			{
				if (value == m_SipUri)
					return;

				m_SipUri = value;

				m_Codec.Logger.LogSetTo(eSeverity.Informational, "SIP URI " + m_Item, m_SipUri);
				OnUriChange.Raise(this, new StringEventArgs(m_SipUri));
			}
		}

		/// <summary>
		/// Gets the SIP proxy address.
		/// </summary>
		[PublicAPI]
		public string ProxyAddress
		{
			get { return m_SipProxyAddress; }
			private set
			{
				if (value == m_SipProxyAddress)
					return;

				m_SipProxyAddress = value;

				m_Codec.Logger.LogSetTo(eSeverity.Informational, "SIP Proxy Address " + m_Item, m_SipProxyAddress);
				
				OnProxyAddressChanged.Raise(this, new StringEventArgs(m_SipProxyAddress));
			}
		}

		/// <summary>
		/// Gets the Sip proxy status.
		/// </summary>
		[PublicAPI]
		public string ProxyStatus
		{
			get { return m_SipProxyStatus; }
			private set
			{
				if (value == m_SipProxyStatus)
					return;

				m_SipProxyStatus = value;

				m_Codec.Logger.LogSetTo(eSeverity.Informational, "SIP Proxy Status " + m_Item, m_SipProxyStatus);
				
				OnProxyStatusChanged.Raise(this, new StringEventArgs(m_SipProxyStatus));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		/// <param name="item"></param>
		public SipRegistration(CiscoCodecDevice codec, int item)
		{
			m_Codec = codec;
			m_Item = item;
		}

		#region Parsing

		/// <summary>
		/// Update properties from the given xml.
		/// </summary>
		/// <param name="xml"></param>
		public void ParseXml(string xml)
		{
			// Xml may be in the form

			/*
			<Proxy item="1" maxOccurrence="n">
			  <Address></Address>
			  <Status>Off</Status>
			</Proxy>
			*/

			// Or

			/*
			<Registration item="1" maxOccurrence="n">
			  <Reason>DNS lookup failed</Reason>
			  <Status>Failed</Status>
			  <URI>ftt18330017@cisco-device</URI>
			</Registration>
			*/

			string rootElement = XmlUtils.ReadElementName(xml);

			switch (rootElement)
			{
				case "Proxy":
					ParseProxyXml(xml);
					break;

				case "Registration":
					ParseRegistrationXml(xml);
					break;

				default:
					throw new ArgumentException("xml");
			}
		}

		private void ParseProxyXml(string xml)
		{
			/*
			<Proxy item="1" maxOccurrence="n">
			  <Address></Address>
			  <Status>Off</Status>
			</Proxy>
			*/

			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.ReadToNextElement();

				foreach (IcdXmlReader child in reader.GetChildElements())
				{
					switch (child.Name)
					{
						case "Address":
							ProxyAddress = child.ReadElementContentAsString();
							break;

						case "Status":
							ProxyStatus = child.ReadElementContentAsString();
							break;
					}

					child.Dispose();
				}
			}
		}

		private void ParseRegistrationXml(string xml)
		{
			/*
			<Registration item="1" maxOccurrence="n">
			  <Reason>DNS lookup failed</Reason>
			  <Status>Failed</Status>
			  <URI>ftt18330017@cisco-device</URI>
			</Registration>
			*/

			using (IcdXmlReader reader = new IcdXmlReader(xml))
			{
				reader.ReadToNextElement();

				foreach (IcdXmlReader child in reader.GetChildElements())
				{
					switch (child.Name)
					{
						case "Reason":
							Reason = child.ReadElementContentAsString();
							break;

						case "Status":
							Registration = child.ReadElementContentAsEnum<eRegState>(true);
							break;

						case "URI":
							Uri = child.ReadElementContentAsString();
							break;
					}

					child.Dispose();
				}
			}
		}

		#endregion
	}
}
