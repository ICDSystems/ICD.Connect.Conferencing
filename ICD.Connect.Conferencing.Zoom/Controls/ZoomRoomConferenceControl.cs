using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Contacts;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Components.Directory;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls
{
	public sealed class ZoomRoomConferenceControl : AbstractWebConferenceDeviceControl<ZoomRoom>
	{

		public override event EventHandler<ConferenceEventArgs> OnConferenceAdded;
		public override event EventHandler<ConferenceEventArgs> OnConferenceRemoved;

		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved; 

		private readonly CallComponent m_ZoomConference;

		private readonly SafeCriticalSection m_IncomingCallsSection;
		private readonly Dictionary<ThinIncomingCall, SafeTimer> m_IncomingCalls;

		#region Properties

		public override eCallType Supports
		{
			get { return eCallType.Video; }
		}

		#endregion

		public ZoomRoomConferenceControl(ZoomRoom parent, int id) : base(parent, id)
		{
			m_ZoomConference = Parent.Components.GetComponent<CallComponent>();
			Subscribe(m_ZoomConference);
			m_IncomingCalls = new Dictionary<ThinIncomingCall, SafeTimer>();
			m_IncomingCallsSection = new SafeCriticalSection();
			Subscribe(parent);
		}

		protected override void DisposeFinal(bool disposing)
		{
			base.DisposeFinal(disposing);

			Unsubscribe(Parent);
		}

		#region Methods

		public override IEnumerable<IWebConference> GetConferences()
		{
			if(m_ZoomConference.Status == eConferenceStatus.Connected)
				yield return m_ZoomConference;
		}

		public override eDialContextSupport CanDial(IDialContext dialContext)
		{
			if (string.IsNullOrEmpty(dialContext.DialString))
				return eDialContextSupport.Unsupported;
			
			if (dialContext.Protocol == eDialProtocol.Zoom || dialContext.Protocol == eDialProtocol.ZoomContact)
				return eDialContextSupport.Native;

			return eDialContextSupport.Unsupported;
		}

		public override void Dial(IDialContext dialContext)
		{
			if(dialContext.Protocol == eDialProtocol.Zoom)
				StartMeeting(dialContext.DialString);
			else if (dialContext.Protocol == eDialProtocol.ZoomContact)
			{
				if (m_ZoomConference.Status == eConferenceStatus.Connected)
					InviteUser(dialContext.DialString);
				else
					StartPersonalMeetingAndInviteUser(dialContext.DialString);
			}
		}

		public override void SetDoNotDisturb(bool enabled)
		{
			Parent.DoNotDisturb = enabled;
		}

		public override void SetAutoAnswer(bool enabled)
		{
			Parent.Log(eSeverity.Warning, "Zoom Room does not support setting auto-answer through the SSH API");
		}

		public override void SetPrivacyMute(bool enabled)
		{
			Parent.SendCommand("zConfiguration Call Microphone mute: {0}", enabled ? "on" : "off");
			//Parent.SendCommand("zConfiguration Call Camera mute: {0}", enabled ? "on" : "off");
		}

		public void StartPersonalMeeting()
		{
			Parent.Log(eSeverity.Debug, "Starting personal Zoom meeting");
			Parent.SendCommand("zCommand Dial StartPmi Duration: 30");
		}

		#endregion
		
		#region Private Methods

		private void StartMeeting(string meetingNumber)
		{
			Parent.SendCommand("zCommand Dial Start meetingNumber: {0}", meetingNumber);
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
			return new ThinIncomingCall()
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
			return (source) =>
			{
				Parent.SendCommand("zCommand Call Accept callerJid: {0}", call.CallerJoinId);
				source.AnswerState = eCallAnswerState.Answered;
				RemoveIncomingCall(source);
			};
		}

		private ThinIncomingCallRejectCallback IncomingCallRejectCallback(IncomingCall call)
		{
			return (source) =>
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

		private void Subscribe(ZoomRoom parent)
		{
			parent.RegisterResponseCallback<IncomingCallResponse>(IncomingCallCallback);
			parent.RegisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);

			parent.OnInitializedChanged += ParentOnOnInitializedChanged;
		}

		private void Unsubscribe(ZoomRoom parent)
		{
			parent.UnregisterResponseCallback<IncomingCallResponse>(IncomingCallCallback);
			parent.UnregisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);

			parent.OnInitializedChanged += ParentOnOnInitializedChanged;
		}

		private void CallConfigurationCallback(ZoomRoom zoomRoom, CallConfigurationResponse response)
		{
			var configuration = response.CallConfiguration;
			if (configuration.Microphone != null)
				PrivacyMuted = configuration.Microphone.Mute;
		}

		private void ParentOnOnInitializedChanged(object sender, BoolEventArgs e)
		{
			Parent.SendCommand("zConfiguration Call Microphone");
		}

		private void IncomingCallCallback(ZoomRoom zoomroom, IncomingCallResponse response)
		{
			var incomingCall = CreateThinIncomingCall(response.IncomingCall);
			Parent.Log(eSeverity.Informational, "Incoming call: {0}", response.IncomingCall.CallerName);
			AddIncomingCall(incomingCall);
		}

		#endregion

		#region Conference Callbacks

		private void Subscribe(CallComponent conference)
		{
			conference.OnStatusChanged += ConferenceOnCallStatusChanged;
		}

		private void ConferenceOnCallStatusChanged(object sender, ConferenceStatusEventArgs args)
		{
			
		}

		#endregion
	}
}