using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Timers;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Components.System;
using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Connect.Protocol.Network.Ports.Web;

namespace ICD.Connect.Conferencing.Zoom.Controls.Conferencing
{
	public sealed class ZoomRoomConferenceControl : AbstractWebConferenceDeviceControl<ZoomRoom>
	{
		/// <summary>
		/// How long to force the camera to stay muted after a meeting starts
		/// </summary>
		private const long KEEP_CAMERA_MUTED_DUE_TIME = 10 * 1000;

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

		private static readonly Dictionary<string, string> s_PersonalToId;

		private readonly CallComponent m_CallComponent;
		private readonly SystemComponent m_SystemComponent;

		private readonly ZoomWebConference m_Conference;
		private readonly SafeCriticalSection m_IncomingCallsSection;
		private readonly Dictionary<IIncomingCall, SafeTimer> m_IncomingCalls;
		private readonly IcdHashSet<string> m_InviteOnMeetingStart;
		private readonly SafeCriticalSection m_InviteSection;

		private readonly HttpPort m_PersonalIdPort;

		/// <summary>
		/// If true, if camera is enabled/unmuted, it will be forced back to disabled
		/// This is used when "Keep Camera Muted On Entry" is enabled
		/// Since zoom tries to re-enable it after the call connects
		/// </summary>
		private bool m_KeepCameraMuted;

		/// <summary>
		/// Timer to clear "Keep Camera Muted" after the meeting is fully started
		/// </summary>
		private readonly SafeTimer m_KeepCameraMutedResetTimer;

		#region Properties

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Video; } }

		#endregion

		/// <summary>
		/// Static constructor.
		/// </summary>
		static ZoomRoomConferenceControl()
		{
			s_PersonalToId = new Dictionary<string, string>();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="id"></param>
		public ZoomRoomConferenceControl(ZoomRoom parent, int id)
			: base(parent, id)
		{
			m_CallComponent = Parent.Components.GetComponent<CallComponent>();
			m_SystemComponent = Parent.Components.GetComponent<SystemComponent>();

			m_IncomingCalls = new Dictionary<IIncomingCall, SafeTimer>();
			m_IncomingCallsSection = new SafeCriticalSection();
			m_InviteOnMeetingStart = new IcdHashSet<string>();
			m_InviteSection = new SafeCriticalSection();
			m_KeepCameraMutedResetTimer = SafeTimer.Stopped(ResetKeepCameraMuted);
			m_PersonalIdPort = new HttpPort();

			m_Conference = new ZoomWebConference(m_CallComponent);
			m_Conference.OnStatusChanged += ConferenceOnStatusChanged;

			SupportedConferenceFeatures |= eConferenceFeatures.AutoAnswer;
			SupportedConferenceFeatures |= eConferenceFeatures.DoNotDisturb;
			SupportedConferenceFeatures |= eConferenceFeatures.PrivacyMute;
			SupportedConferenceFeatures |= eConferenceFeatures.CameraMute;

			Subscribe(m_CallComponent);
			Subscribe(m_SystemComponent);
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

			base.DisposeFinal(disposing);

			m_Conference.OnStatusChanged -= ConferenceOnStatusChanged;

			Unsubscribe(m_CallComponent);
			Unsubscribe(m_SystemComponent);
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

			if (dialContext.Protocol == eDialProtocol.Zoom || dialContext.Protocol == eDialProtocol.ZoomContact ||
			    dialContext.Protocol == eDialProtocol.ZoomPersonal)
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
						// Easy case - Invite the contact to the current meeting
						case eCallStatus.CONNECTING_MEETING:
						case eCallStatus.IN_MEETING:
							m_CallComponent.InviteUser(dialContext.DialString);
							break;

						// Hard case - Start a new meeting and invite once the meeting starts
						default:
							m_InviteSection.Execute(() => m_InviteOnMeetingStart.Add(dialContext.DialString));
							m_CallComponent.StartPersonalMeeting();
							break;
					}
					break;

				case eDialProtocol.ZoomPersonal:
					string newDialString = ConvertZoomPersonalToZoom(dialContext.DialString);

					// Dial normally through the API
					if (string.IsNullOrEmpty(dialContext.Password))
						m_CallComponent.StartMeeting(newDialString);
					else
						m_CallComponent.JoinMeeting(newDialString, dialContext.Password);
					break;
			}
		}

		/// <summary>
		/// Sets the do-not-disturb enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetDoNotDisturb(bool enabled)
		{
			DoNotDisturb = enabled;
		}

		/// <summary>
		/// Sets the auto-answer enabled state.
		/// </summary>
		/// <param name="enabled"></param>
		public override void SetAutoAnswer(bool enabled)
		{
			AutoAnswer = enabled;
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
			
			// If the user explicitly requests to turn on the camera, turn off KeepCameraMuted
			if (enabled && m_KeepCameraMuted)
			{
				m_KeepCameraMuted = false;
				m_KeepCameraMutedResetTimer.Stop();
			}
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

		/// <summary>
		/// Called when the conference status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ConferenceOnStatusChanged(object sender, ConferenceStatusEventArgs e)
		{
			switch (e.Data)
			{
				case eConferenceStatus.Connected:
					UpdateMuteUserOnEntry();
					MuteCameraOnEntry();
					InviteContacts();
					break;

				case eConferenceStatus.Disconnected:
				case eConferenceStatus.Disconnecting:
					ClearInviteContacts();
					break;
			}
		}

		/// <summary>
		/// Invites the contacts that have been stored for the next meeting start.
		/// </summary>
		private void InviteContacts()
		{
			string[] ids;

			m_InviteSection.Enter();

			try
			{
				ids = m_InviteOnMeetingStart.ToArray();
				m_InviteOnMeetingStart.Clear();
			}
			finally
			{
				m_InviteSection.Leave();
			}

			foreach (string id in ids)
				m_CallComponent.InviteUser(id);
		}

		/// <summary>
		/// Clears the contacts that have been queued for the next meeting start.
		/// </summary>
		private void ClearInviteContacts()
		{
			m_InviteSection.Execute(() => m_InviteOnMeetingStart.Clear());
		}

		/// <summary>
		/// Enables/disables the MuteUserOnEntry feature when we become the host.
		/// </summary>
		private void UpdateMuteUserOnEntry()
		{
			// Enables MuteUserOnEntry via the actual Zoom API - Only available when already in a meeting
			if (m_CallComponent.AmIHost)
				m_CallComponent.EnableMuteUserOnEntry(Parent.MuteParticipantsOnStart);
			
			// Make sure all of the existing participants match the mute state
			foreach (ParticipantInfo participant in m_CallComponent.GetParticipants())
				UpdateMuteUserOnEntry(participant);
		}

		/// <summary>
		/// Enforces the mute state on the given participant.
		/// </summary>
		/// <param name="participantInfo"></param>
		private void UpdateMuteUserOnEntry(ParticipantInfo participantInfo)
		{
			if (participantInfo.IsMyself ||
				!m_CallComponent.AmIHost ||
				!Parent.MuteParticipantsOnStart)
				return;

			m_CallComponent.MuteParticipant(participantInfo.UserId, true);
		}

		private void MuteCameraOnEntry()
		{
			if (!Parent.MuteMyCameraOnStart)
				return;

			SetKeepCameraMuted();
			m_CallComponent.MuteCamera(true);
		}

		private void SetKeepCameraMuted()
		{
			m_KeepCameraMuted = true;
			m_KeepCameraMutedResetTimer.Reset(KEEP_CAMERA_MUTED_DUE_TIME);
		}

		private void ResetKeepCameraMuted()
		{
			m_KeepCameraMuted = false;
		}

		/// <summary>
		/// Gets a zoom meeting id for the given personal id.
		/// </summary>
		/// <param name="personal"></param>
		/// <returns></returns>
		private string ConvertZoomPersonalToZoom(string personal)
		{
			string id;
			if (s_PersonalToId.TryGetValue(personal, out id))
				return id;

			// Regex to pull out the real zoom meeting ID
			const string zoomResponseRegex = "zoom\\.us\\/j\\/(?'meeting'\\d+)";

			// Form the full personal URI from the DialString
			// Example: "https://icdpf.zoom.us/my/P3rS0naL.d1aL5tr1nG"
			WebPortResponse response = m_PersonalIdPort.Get("https://icdpf.zoom.us/my/" + personal);

			Match match = Regex.Match(response.ResponseUrl, zoomResponseRegex);
			return s_PersonalToId[personal] = match.Groups["meeting"].Value;
		}

		#endregion

		#region Incoming Calls

		private WebIncomingCall CreateThinIncomingCall(IncomingCall call)
		{
			return new WebIncomingCall
			{
				Name = call.CallerName,
				Number = call.MeetingNumber,
				AnswerState = eCallAnswerState.Unanswered,
				AnswerCallback = IncomingCallAnswerCallback(call),
				RejectCallback = IncomingCallRejectCallback(call)
			};
		}

		private IncomingCallAnswerCallback IncomingCallAnswerCallback(IncomingCall call)
		{
			return source =>
			       {
					   m_CallComponent.CallAccept(call.CallerJoinId);
				       source.AnswerState = eCallAnswerState.Answered;
				       RemoveIncomingCall(source);
			       };
		}

		private IncomingCallRejectCallback IncomingCallRejectCallback(IncomingCall call)
		{
			return source =>
			       {
					   m_CallComponent.CallReject(call.CallerJoinId);
				       source.AnswerState = eCallAnswerState.Ignored;
				       RemoveIncomingCall(source);
			       };
		}

		private void AddIncomingCall(IIncomingCall incomingCall)
		{
			SafeTimer timer = new SafeTimer(incomingCall.Reject, 1000 * 60, -1);
			m_IncomingCallsSection.Execute(() => m_IncomingCalls.Add(incomingCall, timer));

			OnIncomingCallAdded.Raise(this, new GenericEventArgs<IIncomingCall>(incomingCall));
		}

		private void RemoveIncomingCall(IIncomingCall incomingCall)
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
			callComponent.OnMicrophoneMuteChanged += CallComponentOnMicrophoneMuteChanged;
			callComponent.OnParticipantAdded += CallComponentOnParticipantAdded;
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
			callComponent.OnMicrophoneMuteChanged -= CallComponentOnMicrophoneMuteChanged;
			callComponent.OnParticipantAdded -= CallComponentOnParticipantAdded;
		}

		/// <summary>
		/// Called when there is an incoming call.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnIncomingCall(object sender, GenericEventArgs<IncomingCall> eventArgs)
		{
			if (DoNotDisturb || CallLock)
			{
				m_CallComponent.CallReject(eventArgs.Data.CallerJoinId);
				return;
			}

			if (AutoAnswer)
			{
				m_CallComponent.CallAccept(eventArgs.Data.CallerJoinId);
				return;
			}

			WebIncomingCall incomingCall = CreateThinIncomingCall(eventArgs.Data);
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

			// If KeepCameraMuted is set and camera mute is enabled, disable the camera again
			if (m_KeepCameraMuted && !boolEventArgs.Data)
				SetCameraEnabled(false);
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

		private void CallComponentOnMicrophoneMuteChanged(object sender, BoolEventArgs e)
		{
			PrivacyMuted = m_CallComponent.MicrophoneMute;
		}

		private void CallComponentOnParticipantAdded(object sender, GenericEventArgs<ParticipantInfo> genericEventArgs)
		{
			// This accounts for being late to an existing meeting that we are the
			// host of, and "discovering" the participants on entry.
			UpdateMuteUserOnEntry(genericEventArgs.Data);
		}

		#endregion

		#region SystemComponent Callbacks

		/// <summary>
		/// Subscribe to the system component events.
		/// </summary>
		/// <param name="systemComponent"></param>
		private void Subscribe(SystemComponent systemComponent)
		{
			systemComponent.OnSystemInfoChanged += SystemComponentOnSystemInfoChanged;
		}

		/// <summary>
		/// Unsubscribe from the system component events.
		/// </summary>
		/// <param name="systemComponent"></param>
		private void Unsubscribe(SystemComponent systemComponent)
		{
			systemComponent.OnSystemInfoChanged -= SystemComponentOnSystemInfoChanged;
		}

		/// <summary>
		/// Called when the system info changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void SystemComponentOnSystemInfoChanged(object sender, EventArgs eventArgs)
		{
			SystemInfo info = m_SystemComponent.SystemInfo;
			string meetingNumber = info == null ? null : info.MeetingNumber;

			CallInInfo =
				meetingNumber == null
					? null
					: new DialContext
					{
						Protocol = eDialProtocol.Zoom,
						CallType = eCallType.Video,
						DialString = meetingNumber
					};
		}

		#endregion
	}
}
