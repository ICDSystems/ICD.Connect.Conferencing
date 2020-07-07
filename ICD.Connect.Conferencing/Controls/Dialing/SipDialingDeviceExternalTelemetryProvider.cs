using System;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Providers.External;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public sealed class SipDialingDeviceExternalTelemetryProvider : AbstractExternalTelemetryProvider<ISipDialingDeviceControl>
	{
		/// <summary>
		/// Raised when the local name of the sip dialer changes.
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.SIP_LOCAL_NAME_CHANGED)]
		public event EventHandler<StringEventArgs> OnSipLocalNameChanged;

		/// <summary>
		/// Raised when the sip registration status to or from "OK"
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.SIP_ENABLED_CHANGED)]
		public event EventHandler<BoolEventArgs> OnSipEnabledChanged;

		/// <summary>
		/// Raised when the sip registration status changes values
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.SIP_STATUS_CHANGED)]
		public event EventHandler<StringEventArgs> OnSipStatusChanged;

		private bool m_SipEnabled;
		private string m_SipRegistrationStatus;
		private string m_SipName;

		/// <summary>
		/// Gets a boolean representing if sip is reporting a good registration.
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.SIP_ENABLED, null, DialingTelemetryNames.SIP_ENABLED_CHANGED)]
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
		[PropertyTelemetry(DialingTelemetryNames.SIP_STATUS, null, DialingTelemetryNames.SIP_STATUS_CHANGED)]
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
		[PropertyTelemetry(DialingTelemetryNames.SIP_LOCAL_NAME, null, DialingTelemetryNames.SIP_LOCAL_NAME_CHANGED)]
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

		/// <summary>
		/// Sets the parent telemetry provider that this instance extends.
		/// </summary>
		/// <param name="parent"></param>
		protected override void SetParent(ISipDialingDeviceControl parent)
		{
			base.SetParent(parent);

			m_SipName = parent == null ? null : parent.SipLocalName;
			m_SipRegistrationStatus = parent == null ? null : parent.SipRegistrationStatus;
			m_SipEnabled = parent != null && parent.SipIsRegistered;
		}

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(ISipDialingDeviceControl parent)
		{
			base.Subscribe(parent);

			if (parent == null)
				return;

			parent.OnSipLocalNameChanged += ParentOnSipLocalNameChanged;
			parent.OnSipEnabledChanged += ParentOnSipEnabledStateChanged;
			parent.OnSipRegistrationStatusChanged += ParentOnSipRegistrationStatusChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(ISipDialingDeviceControl parent)
		{
			base.Unsubscribe(parent);

			if (parent == null)
				return;

			parent.OnSipLocalNameChanged -= ParentOnSipLocalNameChanged;
			parent.OnSipEnabledChanged -= ParentOnSipEnabledStateChanged;
			parent.OnSipRegistrationStatusChanged -= ParentOnSipRegistrationStatusChanged;
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