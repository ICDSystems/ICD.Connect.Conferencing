using System;
using System.Linq;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Telemetry;

namespace ICD.Connect.Conferencing.Controls.Dialing
{
	public sealed class DialingDeviceExternalTelemetryProvider : IDialingDeviceExternalTelemetryProvider
	{
		public event EventHandler OnRequestTelemetryRebuild;
		public event EventHandler<BoolEventArgs> OnCallInProgressChanged;
		public event EventHandler<StringEventArgs> OnCallTypeChanged;
		public event EventHandler<StringEventArgs> OnCallNumberChanged;

		private IConferenceDeviceControl m_Parent;

		/// <summary>
		/// Gets whether the dialing device has a call in progress
		/// </summary>
		public bool CallInProgress
		{
			get
			{
				if (m_Parent == null)
					return false;

				return m_Parent.GetConferences().SelectMany(c => c.GetOnlineParticipants()).Any();
			}
		}

		/// <summary>
		/// Gets a comma separated list of the type of each active call on the dialer.
		/// </summary>
		public string CallTypes
		{
			get
			{
				if (m_Parent == null)
					return null;

				return string.Join(", ", m_Parent.GetConferences()
				                                 .SelectMany(c => c.GetOnlineParticipants())
				                                 .Select(s => s.CallType.ToString())
				                                 .ToArray());
			}
		}

		/// <summary>
		/// Gets a comma separated list of the type of each active call on the dialer.
		/// </summary>
		public string CallNumbers
		{
			get
			{
				if (m_Parent == null)
					return null;

				return string.Join(", ", m_Parent.GetConferences()
				                                 .SelectMany(c => c.GetOnlineParticipants())
				                                 .Select(p => GetInformationalNumber(p))
				                                 .Except((string)null)
				                                 .ToArray());
			}
		}

		private static string GetInformationalNumber(IParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			ITraditionalParticipant traditional = participant as ITraditionalParticipant;
			if (traditional != null)
				return traditional.Number;

			IWebParticipant web = participant as IWebParticipant;
			if (web != null)
				return web.Name;

			throw new ArgumentException("Unexpected participant type", "participant");
		}

		public void SetParent(ITelemetryProvider provider)
		{
			if (!(provider is IConferenceDeviceControl))
				throw new InvalidOperationException(
					string.Format("Cannot create external telemetry for provider {0}, " +
					              "Provider must be of type IDialingDeviceControl.", provider));

			if (m_Parent != null)
			{
				m_Parent.OnConferenceAdded += ParentOnConferenceAddedOrRemoved;
				m_Parent.OnConferenceRemoved += ParentOnConferenceAddedOrRemoved;
			}

			m_Parent = (IConferenceDeviceControl)provider;

			if (m_Parent != null)
			{
				m_Parent.OnConferenceAdded -= ParentOnConferenceAddedOrRemoved;
				m_Parent.OnConferenceRemoved -= ParentOnConferenceAddedOrRemoved;
			}
		}

		/// <summary>
		/// Forces all calls on the dialer to end.
		/// </summary>
		public void EndCalls()
		{
			if (m_Parent == null)
				return;

			foreach (IConference conference in m_Parent.GetConferences())
				EndConference(conference);
		}

		private static void EndConference(IConference conference)
		{
			if (conference == null)
				throw new ArgumentNullException("conference");

			ITraditionalConference traditional = conference as ITraditionalConference;
			if (traditional != null)
			{
				traditional.Hangup();
				return;
			}

			IWebConference web = conference as IWebConference;
			if (web != null)
			{
				web.LeaveConference();
				return;
			}

			throw new ArgumentException("Unexpected conference type", "conference");
		}

		private void ParentOnConferenceAddedOrRemoved(object sender, ConferenceEventArgs conferenceEventArgs)
		{
			OnCallInProgressChanged.Raise(this, new BoolEventArgs(CallInProgress));
			OnCallTypeChanged.Raise(this, new StringEventArgs(CallTypes));
			OnCallNumberChanged.Raise(this, new StringEventArgs(CallNumbers));
		}
	}
}
