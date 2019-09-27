using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.EventArguments;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls
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
		/// Raised when the Zoom Room reports a call error.
		/// </summary>
		public event EventHandler<GenericEventArgs<CallConnectError>> OnCallError;

		/// <summary>
		/// Raised when the Zoom Room informs us that a password is required.
		/// </summary>
		public event EventHandler<MeetingNeedsPasswordEventArgs> OnPasswordRequired;

		/// <summary>
		/// Raised when the far end requests a microhpone mute state change.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMicrophoneMuteRequested;

		private readonly CallComponent m_ZoomConference;

		private readonly SafeCriticalSection m_IncomingCallsSection;
		private readonly Dictionary<ThinIncomingCall, SafeTimer> m_IncomingCalls;

		private string m_LastJoinNumber;
		private bool m_RequestedMicrophoneMute;
		private bool m_MicrophoneMuted;

		#region Properties

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Video; } }

		/// <summary>
		/// Gets the current known microphone mute state.
		/// </summary>
		public bool MicrophoneMuted
		{
			get { return m_MicrophoneMuted; }
			private set
			{
				if (value == m_MicrophoneMuted)
					return;

				m_MicrophoneMuted = value;

				// The far end is trying to mute/unmute us
				if (m_MicrophoneMuted != m_RequestedMicrophoneMute)
				{
					// Hack - Zoom will mute and unmute while connecting a call
					if (m_ZoomConference.GetOnlineParticipants().Any())
						OnMicrophoneMuteRequested.Raise(this, new BoolEventArgs(m_MicrophoneMuted));
				}

				PrivacyMuted = m_MicrophoneMuted;
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
			m_ZoomConference = Parent.Components.GetComponent<CallComponent>();
			m_IncomingCalls = new Dictionary<ThinIncomingCall, SafeTimer>();
			m_IncomingCallsSection = new SafeCriticalSection();
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
			OnCallError = null;
			OnPasswordRequired = null;

			base.DisposeFinal(disposing);
		}

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IWebConference> GetConferences()
		{
			yield return m_ZoomConference;
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
						StartMeeting(dialContext.DialString);
					else
						JoinMeeting(dialContext.DialString, dialContext.Password);
					break;

				case eDialProtocol.ZoomContact:
					if (m_ZoomConference.Status == eConferenceStatus.Connected)
						InviteUser(dialContext.DialString);
					else
						StartPersonalMeetingAndInviteUser(dialContext.DialString);
					break;
			}
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			Parent.DoNotDisturb = enabled;
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
			m_RequestedMicrophoneMute = enabled;

			SetMicrophoneMute(enabled);
		}

		/// <summary>
		/// Sets whether the camera should transmit video or not.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetCameraEnabled(bool enabled)
		{
			Parent.SendCommand("zConfiguration Call Camera mute: {0}", enabled ? "off" : "on");
		}

		public void StartPersonalMeeting()
		{
			Parent.Log(eSeverity.Debug, "Starting personal Zoom meeting");
			Parent.SendCommand("zCommand Dial StartPmi Duration: 30");
		}

		#endregion

		#region Private Methods

		private void SetMicrophoneMute(bool mute)
		{
			Parent.SendCommand("zConfiguration Call Microphone mute: {0}", mute ? "on" : "off");
		}

		private void StartMeeting(string meetingNumber)
		{
			m_LastJoinNumber = meetingNumber;

			Parent.SendCommand("zCommand Dial Start meetingNumber: {0}", meetingNumber);
		}

		private void JoinMeeting(string meetingNumber, string meetingPassword)
		{
			m_LastJoinNumber = meetingNumber;

			Parent.SendCommand("zCommand Dial Join meetingNumber: {0} password: {1}", meetingNumber, meetingPassword);
		}

		private void StartPersonalMeetingAndInviteUser(string userJoinId)
		{
			// set up one-time invite on call start
			ZoomRoom.ResponseCallback<InfoResultResponse> inviteContactOnCallStart = null;
			inviteContactOnCallStart = (a, b) =>
			                           {
				                           Parent.UnregisterResponseCallback(inviteContactOnCallStart);
				                           InviteUser(userJoinId);
			                           };
			Parent.RegisterResponseCallback(inviteContactOnCallStart);

			// start meeting
			if (m_ZoomConference.Status == eConferenceStatus.Disconnected)
			{
				Parent.Log(eSeverity.Debug, "Starting personal Zoom meeting");
				StartPersonalMeeting();
			}
		}

		private void InviteUser(string userJoinId)
		{
			Parent.Log(eSeverity.Informational, "Inviting user: {0}", userJoinId);
			Parent.SendCommand("zCommand Call Invite user: {0}", userJoinId);
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
				       Parent.SendCommand("zCommand Call Accept callerJid: {0}", call.CallerJoinId);
				       source.AnswerState = eCallAnswerState.Answered;
				       RemoveIncomingCall(source);
			       };
		}

		private ThinIncomingCallRejectCallback IncomingCallRejectCallback(IncomingCall call)
		{
			return source =>
			       {
				       Parent.SendCommand("zCommand Call Reject callerJid: {0}", call.CallerJoinId);
				       source.AnswerState = eCallAnswerState.Ignored;
				       RemoveIncomingCall(source);
			       };
		}

		private void AddIncomingCall(ThinIncomingCall incomingCall)
		{
			m_IncomingCallsSection.Enter();

			try
			{
				var timer = new SafeTimer(incomingCall.Reject, 1000 * 60, -1L);
				m_IncomingCalls.Add(incomingCall, timer);
				OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(incomingCall));
			}
			finally
			{
				m_IncomingCallsSection.Leave();
			}
		}

		private void RemoveIncomingCall(ThinIncomingCall incomingCall)
		{
			m_IncomingCallsSection.Enter();

			try
			{
				SafeTimer timer;
				if (m_IncomingCalls.TryGetValue(incomingCall, out timer))
				{
					timer.Stop();
					m_IncomingCalls.Remove(incomingCall);
				}

				OnIncomingCallRemoved.Raise(this, new GenericEventArgs<IIncomingCall>(incomingCall));
			}
			finally
			{
				m_IncomingCallsSection.Leave();
			}
		}

		#endregion

		#region Parent Callbacks

		/// <summary>
		/// Subscribe to the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Subscribe(ZoomRoom parent)
		{
			base.Subscribe(parent);

			parent.RegisterResponseCallback<IncomingCallResponse>(IncomingCallCallback);
			parent.RegisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
			parent.RegisterResponseCallback<CallConnectErrorResponse>(CallConnectErrorCallback);
			parent.RegisterResponseCallback<MeetingNeedsPasswordResponse>(MeetingNeedsPasswordCallback);
		}

		/// <summary>
		/// Unsubscribe from the parent events.
		/// </summary>
		/// <param name="parent"></param>
		protected override void Unsubscribe(ZoomRoom parent)
		{
			base.Unsubscribe(parent);

			parent.UnregisterResponseCallback<IncomingCallResponse>(IncomingCallCallback);
			parent.UnregisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
			parent.UnregisterResponseCallback<CallConnectErrorResponse>(CallConnectErrorCallback);
			parent.UnregisterResponseCallback<MeetingNeedsPasswordResponse>(MeetingNeedsPasswordCallback);
		}

		/// <summary>
		/// Called when the Zoom Room reports a call connect error.
		/// </summary>
		/// <param name="zoomRoom"></param>
		/// <param name="response"></param>
		private void CallConnectErrorCallback(ZoomRoom zoomRoom, CallConnectErrorResponse response)
		{
			if (response.Error != null)
				OnCallError.Raise(this, new GenericEventArgs<CallConnectError>(response.Error));
		}

		/// <summary>
		/// Called when the Zoom Room reports a call configuration change.
		/// </summary>
		/// <param name="zoomRoom"></param>
		/// <param name="response"></param>
		private void CallConfigurationCallback(ZoomRoom zoomRoom, CallConfigurationResponse response)
		{
			CallConfiguration configuration = response.CallConfiguration;

			if (configuration.Microphone != null)
				MicrophoneMuted = configuration.Microphone.Mute;

			if (configuration.Camera != null)
				CameraEnabled = !configuration.Camera.Mute;
		}

		/// <summary>
		/// Called when the Zoom Room reports an incoming call.
		/// </summary>
		/// <param name="zoomroom"></param>
		/// <param name="response"></param>
		private void IncomingCallCallback(ZoomRoom zoomroom, IncomingCallResponse response)
		{
			var incomingCall = CreateThinIncomingCall(response.IncomingCall);
			Parent.Log(eSeverity.Informational, "Incoming call: {0}", response.IncomingCall.CallerName);
			AddIncomingCall(incomingCall);
		}

		/// <summary>
		/// Called when
		/// </summary>
		/// <param name="zoomRoom"></param>
		/// <param name="response"></param>
		private void MeetingNeedsPasswordCallback(ZoomRoom zoomRoom, MeetingNeedsPasswordResponse response)
		{
			var meetingNeedsPasswordData = response.MeetingNeedsPassword;

			if (meetingNeedsPasswordData.NeedsPassword)
				Parent.Log(eSeverity.Informational, "Meeting needs password NeedsPassword: {0} Wrong/Retry: {1}",
				           meetingNeedsPasswordData.NeedsPassword, meetingNeedsPasswordData.WrongAndRetry);

			OnPasswordRequired.Raise(this,
			                         new MeetingNeedsPasswordEventArgs(m_LastJoinNumber,
			                                                           meetingNeedsPasswordData.NeedsPassword,
			                                                           meetingNeedsPasswordData.WrongAndRetry));
		}

		#endregion

	}
}
