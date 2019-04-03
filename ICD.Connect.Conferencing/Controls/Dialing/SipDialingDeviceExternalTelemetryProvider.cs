using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Telemetry;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public sealed class SipDialingDeviceExternalTelemetryProvider : ISipDialingDeviceExternalTelemetryProvider
	{
		public event EventHandler OnRequestTelemetryRebuild;
		public event EventHandler<StringEventArgs> OnSipLocalNameChanged;
		public event EventHandler<BoolEventArgs> OnSipEnabledChanged;
		public event EventHandler<StringEventArgs> OnSipStatusChanged;

		private ISipDialingDeviceControl m_Parent;
		private bool m_SipEnabled;
		private string m_SipRegistrationStatus;
		private string m_SipName;

		public void SetParent(ITelemetryProvider provider)
		{
			if (!(provider is ISipDialingDeviceControl))
				throw new InvalidOperationException(
					string.Format("Cannot create external telemetry for provider {0}, " +
								  "Provider must be of type ISipDialingDeviceControl.", provider));

			if (m_Parent != null)
			{
				m_Parent.OnSipLocalNameChanged -= ParentOnSipLocalNameChanged;
				m_Parent.OnSipEnabledChanged -= ParentOnSipEnabledStateChanged;
				m_Parent.OnSipRegistrationStatusChanged -= ParentOnSipRegistrationStatusChanged;
			}

			m_Parent = (ISipDialingDeviceControl)provider;

			if (m_Parent != null)
			{
				m_SipName = m_Parent.SipLocalName;
				m_SipRegistrationStatus = m_Parent.SipRegistrationStatus;
				m_SipEnabled = m_Parent.SipIsRegistered;
				m_Parent.OnSipLocalNameChanged += ParentOnSipLocalNameChanged;
				m_Parent.OnSipEnabledChanged += ParentOnSipEnabledStateChanged;
				m_Parent.OnSipRegistrationStatusChanged += ParentOnSipRegistrationStatusChanged;
			}
		}

		/// <summary>
		/// Gets a boolean representing if sip is reporting a good registration.
		/// </summary>
		public bool SipEnabled 
		{ 
			get { return m_SipEnabled; }
			private set
			{
				if(m_SipEnabled == value)
					return;

				m_SipEnabled = value;

				OnSipEnabledChanged.Raise(this, new BoolEventArgs(m_SipEnabled));
			} 
		}

		/// <summary>
		/// Gets the status of the sip registration
		/// </summary>
		public string SipStatus
		{
			get { return m_SipRegistrationStatus; }
			private set
			{
				if(m_SipRegistrationStatus == value)
					return;

				m_SipRegistrationStatus = value;

				OnSipStatusChanged.Raise(this, new StringEventArgs(m_SipRegistrationStatus));
			}
		}

		/// <summary>
		/// Gets the sip URI for this dialer.
		/// </summary>
		public string SipName
		{
			get { return m_SipName; }
			private set
			{
				if(m_SipName == value)
					return;

				m_SipName = value;

				OnSipLocalNameChanged.Raise(this, new StringEventArgs(m_SipName));
			}
		}

		private void ParentOnSipRegistrationStatusChanged(object sender, StringEventArgs args)
		{
			SipStatus = args.Data;
		}

		private void ParentOnSipEnabledStateChanged(object sender, BoolEventArgs args)
		{
			SipEnabled = args.Data;
		}

		private void ParentOnSipLocalNameChanged(object sender, StringEventArgs args)
		{
			SipName = args.Data;
		}
	}
}