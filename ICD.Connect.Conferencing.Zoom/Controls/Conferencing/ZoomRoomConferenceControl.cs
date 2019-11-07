using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls.Conferencing
{
	public sealed class ZoomRoomConferenceControl : AbstractWebConferenceDeviceControl<ZoomRoom>
	{
		/// <summary>
		/// Raised when a source is added to the conference component.
		/// </summary>
		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;

		/// <summary>
		/// Raised when a source is removed from the conference component.
		/// </summary>
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		/// <summary>
		/// Raised when a source property changes.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;

		/// <summary>
		/// Raised when a source property changes.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		/// <summary>
		/// Raised when the MuteUserOnEntry state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteUserOnEntryChanged;

		private readonly CallComponent m_CallComponent;
		private readonly ZoomWebConference m_Conference;
		private readonly SafeCriticalSection m_IncomingCallsSection;
		private readonly Dictionary<ThinIncomingCall, SafeTimer> m_IncomingCalls;

		private bool m_MuteUserOnEntry;

		#region Properties

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Video; } }

		/// <summary>
		/// When true this control will enable the MuteUserOnEntry feature for the current meeting
		/// and when new meetings are started.
		/// </summary>
		/// <value></value>
		public bool MuteUserOnEntry
		{
			get { return m_MuteUserOnEntry; }
			set
			{
				if (value == m_MuteUserOnEntry)
					return;

				m_MuteUserOnEntry = value;

				UpdateMuteUserOnEntry();

				OnMuteUserOnEntryChanged.Raise(this, new BoolEventArgs(m_MuteUserOnEntry));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ZoomRoomConferenceControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			m_CallComponent = Parent.Components.GetComponent<CallComponent>();
			m_IncomingCalls = new Dictionary<ThinIncomingCall, SafeTimer>();
			m_IncomingCallsSection = new SafeCriticalSection();

			m_Conference = new ZoomWebConference(m_CallComponent);
			m_Conference.OnStatusChanged += ConferenceOnStatusChanged;

			Subscribe(m_CallComponent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnConferenceAdded = null;
			OnConferenceRemoved = null;
			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;
			OnMuteUserOnEntryChanged = null;

			base.DisposeFinal(disposing);

			m_Conference.OnStatusChanged -= ConferenceOnStatusChanged;

			Unsubscribe(m_CallComponent);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IWebConference> GetConferences()
		{
			yield return m_Conference;
		}

		/// <summary>
		/// Returns the level of support the device has for the given dial context.
		/// </summary>
		/// <param name="dialContext"></param>
		/// <returns></returns>
		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			if (string.IsNullOrEmpty(dialContext.DialString))
				return eDialContextSupport.Unsupported;

			if (dialContext.Protocol == eDialProtocol.Zoom || dialContext.Protocol == eDialProtocol.ZoomContact)
				return eDialContextSupport.Native;

			return eDialContextSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given context.
		/// </summary>
		/// <param name="dialContext"></param>
		public override void Dial(IDialContext dialContext)
		{
			switch (dialContext.Protocol)
			{
				case eDialProtocol.Zoom:
					if (string.IsNullOrEmpty(dialContext.Password))
						m_CallComponent.StartMeeting(dialContext.DialString);
					else
						m_CallComponent.JoinMeeting(dialContext.DialString, dialContext.Password);
					break;

				case eDialProtocol.ZoomContact:
					switch (m_CallComponent.Status)
					{
						case eCallStatus.CONNECTING_MEETING:
						case eCallStatus.IN_MEETING:
							m_CallComponent.InviteUser(dialContext.DialString);
							break;

						default:
							StartPersonalMeetingAndInviteUser(dialContext.DialString);
							break;
					}
					break;
			}
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			Parent.Log(eSeverity.Warning, "Zoom Room does not support setting do-not-disturb through the SSH API");
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			Parent.Log(eSeverity.Warning, "Zoom Room does not support setting auto-answer through the SSH API");
		}

		/// <summary>
		/// Sets the privacy mute enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetPrivacyMute(bool enabled)
		{
			m_CallComponent.MuteMicrophone(enabled);
		}

		/// <summary>
		/// Sets whether the camera should transmit video or not.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetCameraEnabled(bool enabled)
		{
			m_CallComponent.MuteCamera(!enabled);
		}

		/// <summary>
		/// Starts a personal meeting.
		/// </summary>
		public override void StartPersonalMeeting()
		{
			m_CallComponent.StartPersonalMeeting();
		}

		/// <summary>
		/// Locks the current active conference so no more participants may join.
		/// </summary>
		/// <param name="enabled"></param>
		public override void EnableCallLock(bool enabled)
		{
			m_CallComponent.EnableCallLock(enabled);
		}

		#endregion

		#region Private Methods

		private void StartPersonalMeetingAndInviteUser(string userJoinId)
		{
			// set up one-time invite on call start
			ZoomRoom.ResponseCallback<InfoResultResponse> inviteContactOnCallStart = null;
			inviteContactOnCallStart = (a, b) =>
			                           {
				                           Parent.UnregisterResponseCallback(inviteContactOnCallStart);
				                           m_CallComponent.InviteUser(userJoinId);
			                           };
			Parent.RegisterResponseCallback(inviteContactOnCallStart);

			// start meeting
			if (m_Conference.Status == eConferenceStatus.Disconnected)
			{
				Parent.Log(eSeverity.Debug, "Starting personal Zoom meeting");
				StartPersonalMeeting();
			}
		}

		/// <summary>
		/// Called when the conference status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ConferenceOnStatusChanged(object sender, ConferenceStatusEventArgs e)
		{
			if (e.Data == eConferenceStatus.Connected)
				UpdateMuteUserOnEntry();
		}

		/// <summary>
		/// Enables/disables the MuteUserOnEntry feature when we become the host.
		/// </summary>
		private void UpdateMuteUserOnEntry()
		{
			if (m_CallComponent.AmIHost)
				m_CallComponent.EnableMuteUserOnEntry(m_MuteUserOnEntry);
		}

		#endregion

		#region Incoming Calls

		private ThinIncomingCall CreateThinIncomingCall(IncomingCall call)
		{
			return new ThinIncomingCall
			{
				Name = call.CallerName,
				Number = call.MeetingNumber,
				AnswerState = eCallAnswerState.Unanswered,
				Direction = eCallDirection.Incoming,
				AnswerCallback = IncomingCallAnswerCallback(call),
				RejectCallback = IncomingCallRejectCallback(call)
			};
		}

		private ThinIncomingCallAnswerCallback IncomingCallAnswerCallback(IncomingCall call)
		{
			return source =>
			       {
					   m_CallComponent.CallAccept(call.CallerJoinId);
				       source.AnswerState = eCallAnswerState.Answered;
				       RemoveIncomingCall(source);
			       };
		}

		private ThinIncomingCallRejectCallback IncomingCallRejectCallback(IncomingCall call)
		{
			return source =>
			       {
					   m_CallComponent.CallReject(call.CallerJoinId);
				       source.AnswerState = eCallAnswerState.Ignored;
				       RemoveIncomingCall(source);
			       };
		}

		private void AddIncomingCall(ThinIncomingCall incomingCall)
		{
			SafeTimer timer = new SafeTimer(incomingCall.Reject, 1000 * 60, -1);
			m_IncomingCallsSection.Execute(() => m_IncomingCalls.Add(incomingCall, timer));

			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(incomingCall));
		}

		private void RemoveIncomingCall(ThinIncomingCall incomingCall)
		{
			m_IncomingCallsSection.Enter();

			try
			{
				SafeTimer timer;
				if (!m_IncomingCalls.TryGetValue(incomingCall, out timer))
					return;

				timer.Dispose();
				m_IncomingCalls.Remove(incomingCall);
			}
			finally
			{
				m_IncomingCallsSection.Leave();
			}

			OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(incomingCall));
		}

		#endregion

		#region CallComponent Callbacks

		/// <summary>
		/// Subscribe to the call component events.
		/// </summary>
		/// <param name="callComponent"></param>
		private void Subscribe(CallComponent callComponent)
		{
			callComponent.OnIncomingCall += CallComponentOnIncomingCall;
			callComponent.OnCameraMuteChanged += CallComponentOnCameraMuteChanged;
			callComponent.OnAmIHostChanged += CallComponentOnAmIHostChanged;
			callComponent.OnCallLockChanged += CallComponentOnCallLockChanged;
		}

		/// <summary>
		/// Unsubscribe from the call component events.
		/// </summary>
		/// <param name="callComponent"></param>
		private void Unsubscribe(CallComponent callComponent)
		{
			callComponent.OnIncomingCall -= CallComponentOnIncomingCall;
			callComponent.OnCameraMuteChanged -= CallComponentOnCameraMuteChanged;
			callComponent.OnAmIHostChanged -= CallComponentOnAmIHostChanged;
			callComponent.OnCallLockChanged -= CallComponentOnCallLockChanged;
		}

		/// <summary>
		/// Called when there is an incoming call.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnIncomingCall(object sender, GenericEventArgs<IncomingCall> eventArgs)
		{
			ThinIncomingCall incomingCall = CreateThinIncomingCall(eventArgs.Data);
			AddIncomingCall(incomingCall);
		}

		/// <summary>
		/// Called when the camera mute state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void CallComponentOnCameraMuteChanged(object sender, BoolEventArgs boolEventArgs)
		{
			CameraEnabled = !m_CallComponent.CameraMute;
		}

		/// <summary>
		/// Called when the host state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void CallComponentOnAmIHostChanged(object sender, BoolEventArgs boolEventArgs)
		{
			AmIHost = m_CallComponent.AmIHost;
		}

		/// <summary>
		/// Called when the call lock state changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="boolEventArgs"></param>
		private void CallComponentOnCallLockChanged(object sender, BoolEventArgs boolEventArgs)
		{
			CallLock = m_CallComponent.CallLock;
		}

		#endregion
	}
}
