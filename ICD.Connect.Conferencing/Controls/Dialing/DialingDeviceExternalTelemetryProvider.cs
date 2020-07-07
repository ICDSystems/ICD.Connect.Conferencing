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

			ITraditionalConference traditional = conference as ITraditionalConference;
			if (traditional != null)
			{
				traditional.Hangup();
				return;
			}

			IWebConference web = conference as IWebConference;
			if (web == null)
				throw new ArgumentException("Unexpected conference type", "conference");

			web.LeaveConference();
		}

		private void ParentOnConferenceAddedOrRemoved(object sender, ConferenceEventArgs conferenceEventArgs)
		{
			OnCallInProgressChanged.Raise(this, new BoolEventArgs(CallInProgress));
			OnCallTypeChanged.Raise(this, new StringEventArgs(CallTypes));
			OnCallNumberChanged.Raise(this, new StringEventArgs(CallNumbers));
		}
	}
}
