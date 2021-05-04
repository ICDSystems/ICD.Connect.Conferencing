using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Common.Utils.Timers;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Controls.Dialing;
using ICD.Connect.Conferencing.DialContexts;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Participants.Enums;
using ICD.Connect.Conferencing.Utils;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.Call;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.System;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Components.TraditionalCall;
using ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Responses;
using ICD.Connect.Protocol.Network.Ports.Web;

namespace ICD.Connect.Conferencing.Zoom.Devices.ZoomRooms.Controls.Conferencing
{
	public sealed class ZoomRoomConferenceControl : AbstractConferenceDeviceControl<ZoomRoom, ZoomConference>
	{
		#region Constants

		/// <summary>
		/// How long to force the camera to stay muted after a meeting starts
		/// </summary>
		private const long KEEP_CAMERA_MUTED_DUE_TIME = 10 * 1000;

		#endregion

		#region Events

		/// <summary>
		/// Raised when a source property changes.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallAdded;

		/// <summary>
		/// Raised when a source property changes.
		/// </summary>
		public override event EventHandler<GenericEventArgs<IIncomingCall>> OnIncomingCallRemoved;

		/// <summary>
		/// Raised when Zoom tells us the call out attempt failed.
		/// </summary>
		public event EventHandler<GenericEventArgs<TraditionalZoomPhoneCallInfo>> OnCallOutFailed;

		#endregion

		#region Fields

		private readonly CallComponent m_CallComponent;
		private readonly SystemComponent m_SystemComponent;
		private readonly TraditionalCallComponent m_TraditionalCallComponent;

		private readonly SafeCriticalSection m_InviteSection;
		private readonly SafeCriticalSection m_IncomingCallsSection;
		private readonly SafeCriticalSection m_ParticipantSection;

		/// <summary>
		/// Web based.
		/// </summary>
		private readonly Dictionary<IIncomingCall, SafeTimer> m_IncomingCalls;

		/// <summary>
		/// Traditional outgoing.
		/// </summary>
		private readonly IcdSortedDictionary<string, ThinParticipant> m_CallIdToParticipant;
		private static readonly Dictionary<string, string> s_PersonalToId;
		private readonly IcdHashSet<string> m_InviteOnMeetingStart;

		private readonly ZoomConference m_Conference;

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

		#endregion

		#region Properties

		/// <summary>
		/// Gets the type of conference this dialer supports.
		/// </summary>
		public override eCallType Supports { get { return eCallType.Audio | eCallType.Video; } }

		#endregion

		#region Constructors

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
			m_TraditionalCallComponent = Parent.Components.GetComponent<TraditionalCallComponent>();

			m_IncomingCallsSection = new SafeCriticalSection();
			m_InviteSection = new SafeCriticalSection();
			m_ParticipantSection = new SafeCriticalSection();

			m_IncomingCalls = new Dictionary<IIncomingCall, SafeTimer>();
			m_CallIdToParticipant = new IcdSortedDictionary<string, ThinParticipant>();
			m_InviteOnMeetingStart = new IcdHashSet<string>();
			
			m_KeepCameraMutedResetTimer = SafeTimer.Stopped(ResetKeepCameraMuted);
			m_PersonalIdPort = new HttpPort();

			m_Conference = new ZoomConference(m_CallComponent);
			m_Conference.OnStatusChanged += ConferenceOnStatusChanged;

			SupportedConferenceControlFeatures |= eConferenceControlFeatures.AutoAnswer;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.DoNotDisturb;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.PrivacyMute;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.CameraMute;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.CanDial;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.CanEnd;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.Dtmf;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.CallLock;
			SupportedConferenceControlFeatures |= eConferenceControlFeatures.HostInfoAvailable;

			Subscribe(m_CallComponent);
			Subscribe(m_SystemComponent);
			Subscribe(m_TraditionalCallComponent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void DisposeFinal(bool disposing)
		{
			OnIncomingCallAdded = null;
			OnIncomingCallRemoved = null;
			OnCallOutFailed = null;

			base.DisposeFinal(disposing);

			m_Conference.OnStatusChanged -= ConferenceOnStatusChanged;

			Unsubscribe(m_CallComponent);
			Unsubscribe(m_SystemComponent);
			Unsubscribe(m_TraditionalCallComponent);
		}

		#endregion

		#region Methods

		/// <summary>
		/// Gets the active conference sources.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<ZoomConference> GetConferences()
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

			if (dialContext.Protocol == eDialProtocol.Sip && SipUtils.IsValidSipUri(dialContext.DialString))
				return eDialContextSupport.Supported;

			if (dialContext.Protocol == eDialProtocol.Pstn)
				return eDialContextSupport.Supported;

			return dialContext.Protocol == eDialProtocol.Unknown
				       ? eDialContextSupport.Unknown
				       : eDialContextSupport.Unsupported;
		}

		/// <summary>
		/// Dials the given context.
		/// </summary>
		/// <param name="dialContext"></param>
		public override void Dial(IDialContext dialContext)
		{
			if (CanDial(dialContext) == eDialContextSupport.Unsupported)
				throw new ArgumentException("The specified dial context is unsupported", "dialContext");

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
				case eDialProtocol.Pstn:
				case eDialProtocol.Sip:
					if (string.IsNullOrEmpty(dialContext.DialString) ||
					    dialContext.DialString.Contains('*') ||
					    dialContext.DialString.Contains('#'))
						throw new ArgumentOutOfRangeException("dialContext", "Invalid Dial String");

					PhoneCallOut(dialContext.DialString);
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
		/// Sets the camera mute state.
		/// </summary>
		/// <param name="mute"></param>
		public override void SetCameraMute(bool mute)
		{
			m_CallComponent.MuteCamera(mute);
			
			// If the user explicitly requests to turn on the camera, turn off KeepCameraMuted
			if (!mute && m_KeepCameraMuted)
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

		#region PSTN/SIP

		private void PhoneCallOut(string dialString)
		{
			if (m_CallIdToParticipant.Any())
				throw new InvalidOperationException("Zoom Room only supports singular call out");

			m_TraditionalCallComponent.PhoneCallOut(dialString);
		}

		private void Hangup(string callId)
		{
			if (!m_CallIdToParticipant.Any())
				throw new InvalidOperationException("No active call to hangup");

			m_TraditionalCallComponent.Hangup(callId);
		}

		private void SendDtmf(string callId, char data)
		{
			if (!m_CallIdToParticipant.Any())
				throw new InvalidOperationException("No active call to send DTMF data to");

			m_TraditionalCallComponent.SendDtmf(callId, data);
		}

		/// <summary>
		/// Creates and/or updates a participant from the given call info.
		/// </summary>
		/// <param name="info"></param>
		private void CreateOrUpdateCall(TraditionalZoomPhoneCallInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			ThinParticipant value;

			m_ParticipantSection.Enter();

			try
			{
				if (!m_CallIdToParticipant.TryGetValue(info.CallId, out value))
				{
					value = new ThinParticipant();
					m_CallIdToParticipant.Add(info.CallId, value);
					Subscribe(value);
				}

				UpdateCall(info);
			}
			finally
			{
				m_ParticipantSection.Leave();
			}

			AddParticipant(value);
		}

		/// <summary>
		/// Removes the participants from the call info Id.
		/// </summary>
		/// <param name="info"></param>
		private void RemoveCall(TraditionalZoomPhoneCallInfo info)
		{
			ThinParticipant value;

			m_ParticipantSection.Enter();

			try
			{
				if (!m_CallIdToParticipant.TryGetValue(info.CallId, out value))
					return;

				value.SetEnd(value.EndTime ?? IcdEnvironment.GetUtcTime());
				value.SetStatus(eParticipantStatus.Disconnected);

				Unsubscribe(value);

				m_CallIdToParticipant.Remove(info.CallId);
			}
			finally
			{
				m_ParticipantSection.Leave();
			}

			RemoveParticipant(value);
		}

		/// <summary>
		/// Updates the participant with the given call info.
		/// </summary>
		/// <param name="info"></param>
		private void UpdateCall(TraditionalZoomPhoneCallInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			ThinParticipant value = m_ParticipantSection.Execute(() => m_CallIdToParticipant.GetDefault(info.CallId));
			if (value == null)
				return;

			value.SetNumber(info.PeerNumber);
			value.SetName(info.PeerDisplayName);
			value.SetDirection(info.IsIncomingCall ? eCallDirection.Incoming : eCallDirection.Outgoing);
			value.SetStatus(GetStatus(info.Status));
			value.SetCallType(Supports);

			if (value.GetIsOnline())
				value.SetStart(value.StartTime ?? IcdEnvironment.GetUtcTime());
		}

		/// <summary>
		/// Gets the participant status based on the zoom call status.
		/// </summary>
		/// <param name="status"></param>
		/// <returns></returns>
		private static eParticipantStatus GetStatus(eZoomPhoneCallStatus status)
		{
			switch (status)
			{
				case eZoomPhoneCallStatus.None:
				case eZoomPhoneCallStatus.NotFound:
				case eZoomPhoneCallStatus.Incoming:
					return eParticipantStatus.Undefined;
				case eZoomPhoneCallStatus.Ringing:
					return eParticipantStatus.Ringing;
				case eZoomPhoneCallStatus.Init:
					return eParticipantStatus.Connecting;
				case eZoomPhoneCallStatus.InCall:
					return eParticipantStatus.Connected;
				default:
					throw new ArgumentOutOfRangeException("status");
			}
		}

		#endregion

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
			CameraMute = m_CallComponent.CameraMute;

			// If KeepCameraMuted is set and camera mute is enabled, disable the camera again
			if (m_KeepCameraMuted && boolEventArgs.Data)
				SetCameraMute(false);
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

		#region TraditionalCallComponent Callbacks

		private void Subscribe(TraditionalCallComponent traditionalCallComponent)
		{
			traditionalCallComponent.OnCallStatusChanged += CallComponentOnCallStatusChanged;
			traditionalCallComponent.OnCallTerminated += CallComponentOnCallTerminated;
		}

		private void Unsubscribe(TraditionalCallComponent traditionalCallComponent)
		{
			traditionalCallComponent.OnCallStatusChanged -= CallComponentOnCallStatusChanged;
			traditionalCallComponent.OnCallTerminated -= CallComponentOnCallTerminated;
		}

		private void CallComponentOnCallStatusChanged(object sender, GenericEventArgs<TraditionalZoomPhoneCallInfo> args)
		{
			TraditionalZoomPhoneCallInfo data = args.Data;
			if (data == null)
				return;

			switch (data.Status)
			{
				case eZoomPhoneCallStatus.None:
				case eZoomPhoneCallStatus.NotFound:
					break;

				case eZoomPhoneCallStatus.CallOutFailed:
					Parent.Logger.Log(eSeverity.Warning, "ZoomRoom PSTN Call Out Failed!");
					OnCallOutFailed.Raise(this, new GenericEventArgs<TraditionalZoomPhoneCallInfo>(data));
					break;

				// Zoom doesn't support answering incoming calls so we pretend they don't exist
				case eZoomPhoneCallStatus.Incoming:
					break;

				case eZoomPhoneCallStatus.Ringing:
				case eZoomPhoneCallStatus.Init:
				case eZoomPhoneCallStatus.InCall:
					CreateOrUpdateCall(data);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private void CallComponentOnCallTerminated(object sender, GenericEventArgs<TraditionalZoomPhoneCallInfo> args)
		{
			TraditionalZoomPhoneCallInfo data = args.Data;
			if (data != null)
				RemoveCall(data);
		}

		#endregion

		#region PSTN/SIP Participant Callbacks

		/// <summary>
		/// Subscribe to the participant events.
		/// </summary>
		/// <param name="value"></param>
		private void Subscribe(ThinParticipant value)
		{
			value.HangupCallback = HangupCallback;
			value.SendDtmfCallback = SendDtmfCallback;
			value.HoldCallback = HoldCallback;
			value.ResumeCallback = ResumeCallback;
		}

		/// <summary>
		/// Unsubscribe from the participant events.
		/// </summary>
		/// <param name="value"></param>
		private void Unsubscribe(ThinParticipant value)
		{
			value.HangupCallback = null;
			value.SendDtmfCallback = null;
			value.HoldCallback = null;
			value.ResumeCallback = null;
		}

		private void SendDtmfCallback(ThinParticipant sender, string data)
		{
			string callId = GetIdForParticipant(sender);

			foreach (char index in data)
				SendDtmf(callId, index);
		}

		private void HangupCallback(ThinParticipant sender)
		{
			string callId = GetIdForParticipant(sender);

			Hangup(callId);
		}

		private string GetIdForParticipant(ThinParticipant value)
		{
			return m_ParticipantSection.Execute(() => m_CallIdToParticipant.GetKey(value));
		}

		private void HoldCallback(ThinParticipant sender)
		{
			Parent.Logger.Log(eSeverity.Warning, "Zoom Room PSTN does not support call hold/resume feature");
		}

		private void ResumeCallback(ThinParticipant sender)
		{
			Parent.Logger.Log(eSeverity.Warning, "Zoom Room PSTN does not support call hold/resume feature");
		}

		#endregion
	}
}
