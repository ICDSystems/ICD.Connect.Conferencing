using System;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Telemetry.Attributes;
using ICD.Connect.Telemetry.Providers.External;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public sealed class DialingDeviceExternalTelemetryProvider : AbstractExternalTelemetryProvider<IConferenceDeviceControl>
	{
		#region Events

		/// <summary>
		/// Raised when the dialing device starts a call from idle state or ends the last remaining call
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.CALL_IN_PROGRESS_CHANGED)]
		public event EventHandler<BoolEventArgs> OnCallInProgressChanged;

		/// <summary>
		/// Raised when the dialing device adds or removes a call.
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.CALL_TYPE_CHANGED)]
		public event EventHandler<StringEventArgs> OnCallTypeChanged;

		/// <summary>
		/// Raised when the dialing device adds or removes a call.
		/// </summary>
		[EventTelemetry(DialingTelemetryNames.CALL_NUMBER_CHANGED)]
		public event EventHandler<StringEventArgs> OnCallNumberChanged;

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

		#endregion

		#region Backing Fields

		private bool m_SipEnabled;
		private string m_SipRegistrationStatus;
		private string m_SipName;

		#endregion

		#region Properties

		/// <summary>
		/// Gets whether the dialing device has a call in progress
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.CALL_IN_PROGRESS, null, DialingTelemetryNames.CALL_IN_PROGRESS_CHANGED)]
		public bool CallInProgress
		{
			get { return Parent != null && Parent.GetConferences().SelectMany(c => c.GetOnlineParticipants()).Any(); }
		}

		/// <summary>
		/// Gets a comma separated list of the type of each active call on the dialer.
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.CALL_TYPE, null, DialingTelemetryNames.CALL_TYPE_CHANGED)]
		public string CallTypes
		{
			get
			{
				if (Parent == null)
					return null;

				return string.Join(", ", Parent.GetConferences()
				                               .SelectMany(c => c.GetOnlineParticipants())
				                               .Select(s => s.CallType.ToString())
				                               .ToArray());
			}
		}

		/// <summary>
		/// Gets a comma separated list of the type of each active call on the dialer.
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.CALL_NUMBER, null, DialingTelemetryNames.CALL_NUMBER_CHANGED)]
		public string CallNumbers
		{
			get
			{
				if (Parent == null)
					return null;

				return string.Join(", ", Parent.GetConferences()
				                               .SelectMany(c => c.GetOnlineParticipants())
				                               .Select(p => GetInformationalNumber(p))
				                               .Except((string)null)
				                               .ToArray());
			}
		}

		/// <summary>
		/// Gets a boolean representing if sip is reporting a good registration.
		/// </summary>
		[PropertyTelemetry(DialingTelemetryNames.SIP_ENABLED, null, DialingTelemetryNames.SIP_ENABLED_CHANGED)]
		public bool SipEnabled
		{
			get { return m_SipEnabled; }
			private set
			{
				if (m_SipEnabled == value)
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
				if (m_SipRegistrationStatus == value)
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
				if (m_SipName == value)
					return;

				m_SipName = value;

				OnSipLocalNameChanged.Raise(this, new StringEventArgs(m_SipName));
			}
		}

		#endregion

		#region Methods

		private static string GetInformationalNumber(IParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			// TODO Number vs Name

			return participant.Number;
			return participant.Name;
		}

		/// <summary>
		/// Forces all calls on the dialer to end.
		/// </summary>
		[MethodTelemetry(DialingTelemetryNames.END_CALL_COMMAND)]
		public void EndCalls()
		{
			if (Parent == null)
				return;

			foreach (IConference conference in Parent.GetConferences())
				EndConference(conference);
		}

		private static void EndConference(IConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			//TODO Hangup vs Leave

			conference.Hangup();
			conference.LeaveConference();
		}

		/// <summary>
		/// Sets the parent telemetry provider that this instance extends.
		/// </summary>
		/// <param name="parent"></param>
		protected override void SetParent(IConferenceDeviceControl parent)
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
		protected override void Subscribe(IConferenceDeviceControl parent)
		{
			base.Subscribe(parent);

			if (parent == null)
				return;

			parent.OnConferenceAdded += ParentOnConferenceAddedOrRemoved;
			parent.OnConferenceRemoved += ParentOnConferenceAddedOrRemoved;
			parent.OnSipLocalNameChanged += ParentOnSipLocalNameChanged;
			parent.OnSipEnabledChanged += ParentOnSipEnabledStateChanged;
			parent.OnSipRegistrationStatusChanged += ParentOnSipRegistrationStatusChanged;
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(IConferenceDeviceControl parent)
		{
			base.Unsubscribe(parent);

			if (parent == null)
				return;

			parent.OnConferenceAdded -= ParentOnConferenceAddedOrRemoved;
			parent.OnConferenceRemoved -= ParentOnConferenceAddedOrRemoved;
			parent.OnSipLocalNameChanged -= ParentOnSipLocalNameChanged;
			parent.OnSipEnabledChanged -= ParentOnSipEnabledStateChanged;
			parent.OnSipRegistrationStatusChanged -= ParentOnSipRegistrationStatusChanged;
		}

		private void ParentOnConferenceAddedOrRemoved(object sender, ConferenceEventArgs conferenceEventArgs)
		{
			OnCallInProgressChanged.Raise(this, new BoolEventArgs(CallInProgress));
			OnCallTypeChanged.Raise(this, new StringEventArgs(CallTypes));
			OnCallNumberChanged.Raise(this, new StringEventArgs(CallNumbers));
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

		#endregion
	}
}
