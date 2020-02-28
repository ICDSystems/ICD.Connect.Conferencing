using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Zoom.EventArguments;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	public sealed class CallComponent : AbstractZoomRoomComponent
	{
		#region Events

		/// <summary>
		/// Raised when a participant is removed from the conference.
		/// </summary>
		public event EventHandler<GenericEventArgs<ParticipantInfo>> OnParticipantRemoved;

		/// <summary>
		/// Raised when a participant is added to the conference.
		/// </summary>
		public event EventHandler<GenericEventArgs<ParticipantInfo>> OnParticipantAdded;

		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		public event EventHandler<GenericEventArgs<eCallStatus>> OnStatusChanged;

		/// <summary>
		/// Raised when the call lock status changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCallLockChanged;

		/// <summary>
		/// Raised when the zoom room informs us the call record status has changed.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCallRecordChanged;

		/// <summary>
		/// Raised when the zoom room attempts to record the call but is not able to.
		/// </summary>
		public event EventHandler<StringEventArgs> OnCallRecordErrorState; 

		/// <summary>
		/// Raised when the meeting id changes.
		/// </summary>
		public event EventHandler<StringEventArgs> OnMeetingIdChanged;

		/// <summary>
		/// Raised when the microphone mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMicrophoneMuteChanged;

		/// <summary>
		/// Raised when the far end requests a microphone mute state change.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnFarEndRequestedMicrophoneMute;

		/// <summary>
		/// Raised when the camera mute state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCameraMuteChanged;

		/// <summary>
		/// Raised when the far end sends a video un-mute request.
		/// </summary>
		public event EventHandler OnFarEndRequestedVideoUnMute;

		/// <summary>
		/// Raised when we start/stop being the host of the active conference.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnAmIHostChanged;

		/// <summary>
		/// Raised when the Zoom Room reports a call error.
		/// </summary>
		public event EventHandler<GenericEventArgs<CallConnectError>> OnCallError;

		/// <summary>
		/// Raised when the Zoom Room informs us that a password is required.
		/// </summary>
		public event EventHandler<MeetingNeedsPasswordEventArgs> OnPasswordRequired;

		/// <summary>
		/// Raised when the state of mute user on entry is changed.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnMuteUserOnEntryChanged;

		/// <summary>
		/// Raised when there is an incoming call.
		/// </summary>
		public event EventHandler<GenericEventArgs<IncomingCall>> OnIncomingCall;

		/// <summary>
		/// Raised when the Call Record info updates.
		/// </summary>
		public event EventHandler<GenericEventArgs<UpdateCallRecordInfoEvent>> OnUpdatedCallRecordInfo;

		/// <summary>
		/// Raised when joining a call lobby that requires the host to start the call.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnNeedWaitForHost;

		#endregion

		private readonly IcdOrderedDictionary<string, ParticipantInfo> m_Participants;
		private readonly SafeCriticalSection m_ParticipantsSection;

		private bool m_CallLock;
		private bool m_CallRecord;
		private string m_MeetingId;
		private eCallStatus m_Status;
		private bool m_CameraMute;
		private bool m_MicrophoneMute;
		private bool m_AmIHost;
		private bool m_MuteUserOnEntry;

		// We track the last number we tried to join in order to give password feedback
		private string m_LastJoinNumber;

		// We track the last time we set our microphone mute state so we can determine if the
		// far end is trying to mute/unmute us.
		private bool m_LastMicrophoneMute;

		#region Properties

		/// <summary>
		/// Gets the meeting ID.
		/// </summary>
		public string MeetingId
		{
			get { return m_MeetingId; }
			private set
			{
				if (value == m_MeetingId)
					return;

				m_MeetingId = value;
				Parent.Log(eSeverity.Informational, "MeetingId changed to {0}", m_MeetingId);

				OnMeetingIdChanged.Raise(this, new StringEventArgs(m_MeetingId));
			}
		}

		/// <summary>
		/// Gets the camera mute state.
		/// </summary>
		public bool CameraMute
		{ 
			get { return m_CameraMute; }
			private set
			{
				if (value == m_CameraMute)
					return;

				m_CameraMute = value;
				Parent.Log(eSeverity.Informational, "CameraMute changed to {0}", m_CameraMute);

				OnCameraMuteChanged.Raise(this, new BoolEventArgs(m_CameraMute));
			}
		}

		/// <summary>
		/// Gets the microphone mute state.
		/// </summary>
		public bool MicrophoneMute
		{
			get { return m_MicrophoneMute; }
			private set
			{
				if (value == m_MicrophoneMute)
					return;

				m_MicrophoneMute = value;
				Parent.Log(eSeverity.Informational, "MicrophoneMute changed to {0}", m_MicrophoneMute);

				// The far end is trying to mute/unmute us
				if (m_MicrophoneMute != m_LastMicrophoneMute)
				{
					// Hack - Zoom will mute and unmute while connecting a call
					if (GetParticipants().Any(p => !p.IsMyself))
						OnFarEndRequestedMicrophoneMute.Raise(this, new BoolEventArgs(m_MicrophoneMute));
				}

				OnMicrophoneMuteChanged.Raise(this, new BoolEventArgs(m_MicrophoneMute));
			}
		}

		/// <summary>
		/// Returns true if we are the host of the current conference.
		/// </summary>
		public bool AmIHost
		{
			get { return m_AmIHost; }
			private set
			{
				if (value == m_AmIHost)
					return;

				m_AmIHost = value;
				Parent.Log(eSeverity.Informational, "AmIHost changed to {0}", m_AmIHost);

				OnAmIHostChanged.Raise(this, new BoolEventArgs(m_AmIHost));
			}
		}

		/// <summary>
		/// Gets the CallLock State.
		/// </summary>
		public bool CallLock
		{
			get { return m_CallLock; }
			private set
			{
				if (value == m_CallLock)
					return;

				m_CallLock = value;
				Parent.Log(eSeverity.Informational, "CallLock set to {0}", m_CallLock);

				OnCallLockChanged.Raise(this, new BoolEventArgs(m_CallLock));
			}
		}

		/// <summary>
		/// Gets the Call Record State.
		/// </summary>
		public bool CallRecord
		{
			get { return m_CallRecord; }
			private set
			{
				if (value == m_CallRecord)
					return;

				m_CallRecord = value;
				Parent.Log(eSeverity.Informational, "CallRecord set to {0}", m_CallRecord);

				OnCallRecordChanged.Raise(this, new BoolEventArgs(m_CallRecord));
			}
		}

		/// <summary>
		/// Gets the current conference status.
		/// </summary>
		public eCallStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (value == m_Status)
					return;

				m_Status = value;
				Parent.Log(eSeverity.Informational, "Status changed to {0}", m_Status);

				OnStatusChanged.Raise(this, new GenericEventArgs<eCallStatus>(m_Status));
			}
		}

		/// <summary>
		/// Whether or not participants are being muted upon joining the current meeting.
		/// </summary>
		public bool MuteUserOnEntry
		{
			get { return m_MuteUserOnEntry; }
			private set
			{
				if (value == m_MuteUserOnEntry)
					return;

				m_MuteUserOnEntry = value;
				Parent.Log(eSeverity.Informational, "MuteUserOnEntry changed to {0}", m_MuteUserOnEntry);

				OnMuteUserOnEntryChanged.Raise(this, new BoolEventArgs(m_MuteUserOnEntry));
			}
		}

		public CallInfo CallInfo { get; private set; }

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public CallComponent(ZoomRoom parent)
			: base(parent)
		{
			m_Participants = new IcdOrderedDictionary<string, ParticipantInfo>();
			m_ParticipantsSection = new SafeCriticalSection();

			Subscribe(Parent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal()
		{
			OnParticipantRemoved = null;
			OnParticipantAdded = null;
			OnStatusChanged = null;
			OnCallLockChanged = null;
			OnCallRecordChanged = null;
			OnMeetingIdChanged = null;
			OnMicrophoneMuteChanged = null;
			OnFarEndRequestedMicrophoneMute = null;
			OnCameraMuteChanged = null;
			OnFarEndRequestedVideoUnMute = null;
			OnAmIHostChanged = null;
			OnCallError = null;
			OnPasswordRequired = null;
			OnMuteUserOnEntryChanged = null;
			OnIncomingCall = null;
			OnUpdatedCallRecordInfo = null;

			base.DisposeFinal();

			Unsubscribe(Parent);
		}

		#endregion

		#region Methods

		public IEnumerable<ParticipantInfo> GetParticipants()
		{
			return m_ParticipantsSection.Execute(() => m_Participants.Values.ToArray());
		}

		/// <summary>
		/// Leaves the current active conference.
		/// </summary>
		public void CallLeave()
		{
			Parent.Log(eSeverity.Debug, "Leaving Zoom Meeting {0}", MeetingId);
			Parent.SendCommand("zCommand Call Leave");
		}

		/// <summary>
		/// Ends the current active conference for all participants.
		/// </summary>
		public void CallDisconnect()
		{
			Parent.Log(eSeverity.Debug, "Ending Zoom Meeting {0}", MeetingId);
			Parent.SendCommand("zCommand Call Disconnect");
		}

		/// <summary>
		/// Accepts the incoming call with the given id.
		/// </summary>
		/// <param name="joinId"></param>
		public void CallAccept(string joinId)
		{
			Parent.Log(eSeverity.Debug, "Accepting incoming call {0}", joinId);
			Parent.SendCommand("zCommand Call Accept callerJid: {0}", joinId);
		}

		/// <summary>
		/// Rejects the incoming call with the given id.
		/// </summary>
		/// <param name="joinId"></param>
		public void CallReject(string joinId)
		{
			Parent.Log(eSeverity.Debug, "Rejecting incoming call {0}", joinId);
			Parent.SendCommand("zCommand Call Reject callerJid: {0}", joinId);
		}

		/// <summary>
		/// Invites the user with the given id to the current conference.
		/// </summary>
		/// <param name="joinId"></param>
		public void InviteUser(string joinId)
		{
			Parent.Log(eSeverity.Informational, "Inviting user: {0}", joinId);
			Parent.SendCommand("zCommand Call Invite user: {0}", joinId);
		}

		/// <summary>
		/// Expels the participant with the given user id.
		/// </summary>
		/// <param name="userId"></param>
		public void ExpelParticipant(string userId)
		{
			Parent.Log(eSeverity.Informational, "Expelling participant with id: {0}", userId);
			Parent.SendCommand("zCommand Call Expel Id: {0}", userId);
		}

		/// <summary>
		/// Mutes/unmutes the participant with the given user id.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="mute"></param>
		public void MuteParticipant(string userId, bool mute)
		{
			Parent.Log(eSeverity.Informational, "{0} participant with id: {1}", mute ? "Muting" : "Unmuting", userId);
			Parent.SendCommand("zCommand Call MuteParticipant mute: {0} Id: {1}", mute ? "on" : "off", userId);
		}

		/// <summary>
		/// Allows/disallows recording for the participant with the given user id.
		/// </summary>
		/// <param name="userId"></param>
		/// <param name="enabled"></param>
		public void AllowParticipantRecord(string userId, bool enabled)
		{
			Parent.Log(eSeverity.Informational, "Setting Call Record Enable to: {0} for participant: {1}", enabled, userId);
			Parent.SendCommand("zCommand Call AllowRecord Id: {0} Enable: {1}", userId, enabled ? "on" : "off");
		}

		/// <summary>
		/// Locks the current active conference so no more participants may join.
		/// </summary>
		/// <param name="enabled"></param>
		public void EnableCallLock(bool enabled)
		{
			Parent.Log(eSeverity.Debug, "Setting the Call Lock state to: {0}", enabled);
			Parent.SendCommand("zConfiguration Call Lock Enable: {0}", enabled ? "on" : "off");
		}

		/// <summary>
		/// Sets the enabled state of call recording.
		/// </summary>
		/// <param name="enabled"></param>
		public void EnableCallRecord(bool enabled)
		{
			Parent.Log(eSeverity.Debug, "Setting the Call Record state to: {0}", enabled);
			Parent.SendCommand("zCommand Call Record Enable: {0}", enabled ? "on" : "off");
		}

		/// <summary>
		/// Sets the mute state of the camera.
		/// </summary>
		/// <param name="mute"></param>
		public void MuteCamera(bool mute)
		{
			Parent.Log(eSeverity.Debug, "Setting the Call Camera Mute state to: {0}", mute);
			Parent.SendCommand("zConfiguration Call Camera Mute: {0}", mute ? "on" : "off");
		}

		/// <summary>
		/// Starts a personal zoom meeting.
		/// </summary>
		public void StartPersonalMeeting()
		{
			Parent.Log(eSeverity.Debug, "Starting personal Zoom meeting");
			Parent.SendCommand("zCommand Dial StartPmi Duration: 30");
		}

		/// <summary>
		/// Sets whether participants should be muted upon entering a meeting or not.
		/// </summary>
		/// <param name="enabled"></param>
		public void EnableMuteUserOnEntry(bool enabled)
		{
			Parent.Log(eSeverity.Informational, "Setting MuteUserOnEntry to: {0}", enabled);
			Parent.SendCommand("zConfiguration Call MuteUserOnEntry Enable: {0}", enabled ? "on" : "off");
		}

		/// <summary>
		/// Sets the mute state of the microphone.
		/// </summary>
		/// <param name="mute"></param>
		public void MuteMicrophone(bool mute)
		{
			m_LastMicrophoneMute = mute;

			Parent.Log(eSeverity.Debug, "Setting the Microphone Mute state to: {0}", mute);
			Parent.SendCommand("zConfiguration Call Microphone mute: {0}", mute ? "on" : "off");
		}

		/// <summary>
		/// Starts a new meeting with the given number.
		/// </summary>
		/// <param name="meetingNumber"></param>
		public void StartMeeting(string meetingNumber)
		{
			m_LastJoinNumber = meetingNumber;

			Parent.Log(eSeverity.Debug, "Starting a meeting with number: {0}", meetingNumber);
			Parent.SendCommand("zCommand Dial Start meetingNumber: {0}", meetingNumber);
		}

		/// <summary>
		/// Starts an existing meeting with the given number and password.
		/// </summary>
		/// <param name="meetingNumber"></param>
		/// <param name="meetingPassword"></param>
		public void JoinMeeting(string meetingNumber, string meetingPassword)
		{
			m_LastJoinNumber = meetingNumber;

			Parent.Log(eSeverity.Debug, "Joining a meeting with number: {0} and password: {1}", meetingNumber, meetingPassword);
			Parent.SendCommand("zCommand Dial Join meetingNumber: {0} password: {1}", meetingNumber, meetingPassword);
		}

		#endregion

		#region Private Methods

		protected override void Initialize()
		{
			base.Initialize();

			Parent.SendCommand("zStatus Call Status");
			Parent.SendCommand("zConfiguration Call Camera mute");
			Parent.SendCommand("zConfiguration Call Microphone mute");
			Parent.SendCommand("zCommand Call ListParticipants");
			Parent.SendCommand("zCommand Call Info");
		}

		#endregion

		#region Participants

		private void AddUpdateOrRemoveParticipant(ParticipantInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			if (info.IsMyself)
				AmIHost = info.IsHost || info.IsCohost;

			switch (info.Event)
			{
				case eUserChangedEventType.ZRCUserChangedEventLeftMeeting:
					RemoveParticipant(info);
					break;

				default:
					AddOrUpdateParticipant(info);
					break;
			}
		}

		private void AddOrUpdateParticipant(ParticipantInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			m_ParticipantsSection.Execute(() => m_Participants[info.UserId] = info);

			OnParticipantAdded.Raise(this, new GenericEventArgs<ParticipantInfo>(info));
		}

		private void RemoveParticipant(ParticipantInfo info)
		{
			if (info == null)
				throw new ArgumentNullException("info");

			if (!m_ParticipantsSection.Execute(() => m_Participants.Remove(info.UserId)))
				return;

			OnParticipantRemoved.Raise(this, new GenericEventArgs<ParticipantInfo>(info));
		}

		private void ClearParticipants()
		{
			foreach (ParticipantInfo participant in GetParticipants())
				RemoveParticipant(participant);
		}

		#endregion

		#region ZoomRoom Callbacks

		/// <summary>
		/// Subscribe to the ZoomRoom events.
		/// </summary>
		/// <param name="zoomRoom"></param>
		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
			zoomRoom.RegisterResponseCallback<ListParticipantsResponse>(ListParticipantsCallback);
			zoomRoom.RegisterResponseCallback<CallDisconnectResponse>(DisconnectCallback);
			zoomRoom.RegisterResponseCallback<InfoResultResponse>(CallInfoCallback);
			zoomRoom.RegisterResponseCallback<CallStatusResponse>(CallStatusCallback);
			zoomRoom.RegisterResponseCallback<UpdatedCallRecordInfoResponse>(UpdatedCallRecordInfoCallback);
			zoomRoom.RegisterResponseCallback<CallRecordStatusResponse>(CallRecordStatusCallback);
			zoomRoom.RegisterResponseCallback<VideoUnMuteRequestResponse>(VideoUnMuteRequestCallback);
			zoomRoom.RegisterResponseCallback<IncomingCallResponse>(IncomingCallCallback);
			zoomRoom.RegisterResponseCallback<CallConnectErrorResponse>(CallConnectErrorCallback);
			zoomRoom.RegisterResponseCallback<MeetingNeedsPasswordResponse>(MeetingNeedsPasswordCallback);
			zoomRoom.RegisterResponseCallback<NeedWaitForHostResponse>(NeedWaitForHostCallback);
		}

		/// <summary>
		/// Unsubscribe from the ZoomRoom events.
		/// </summary>
		/// <param name="zoomRoom"></param>
		private void Unsubscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.UnregisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
			zoomRoom.UnregisterResponseCallback<ListParticipantsResponse>(ListParticipantsCallback);
			zoomRoom.UnregisterResponseCallback<CallDisconnectResponse>(DisconnectCallback);
			zoomRoom.UnregisterResponseCallback<InfoResultResponse>(CallInfoCallback);
			zoomRoom.UnregisterResponseCallback<CallStatusResponse>(CallStatusCallback);
			zoomRoom.UnregisterResponseCallback<UpdatedCallRecordInfoResponse>(UpdatedCallRecordInfoCallback);
			zoomRoom.UnregisterResponseCallback<CallRecordStatusResponse>(CallRecordStatusCallback);
			zoomRoom.UnregisterResponseCallback<VideoUnMuteRequestResponse>(VideoUnMuteRequestCallback);
			zoomRoom.UnregisterResponseCallback<IncomingCallResponse>(IncomingCallCallback);
			zoomRoom.UnregisterResponseCallback<CallConnectErrorResponse>(CallConnectErrorCallback);
			zoomRoom.UnregisterResponseCallback<MeetingNeedsPasswordResponse>(MeetingNeedsPasswordCallback);
			zoomRoom.UnregisterResponseCallback<NeedWaitForHostResponse>(NeedWaitForHostCallback);
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
			if (configuration == null)
				return;

			if (configuration.MuteUserOnEntry != null)
				MuteUserOnEntry = configuration.MuteUserOnEntry.Enabled;

			if (configuration.Microphone != null)
				MicrophoneMute = configuration.Microphone.Mute;

			if (configuration.Camera != null)
				CameraMute = configuration.Camera.Mute;

			if (configuration.CallLockStatus != null)
				CallLock = configuration.CallLockStatus.Lock;
		}

		/// <summary>
		/// Called when the Zoom Room reports an incoming call.
		/// </summary>
		/// <param name="zoomroom"></param>
		/// <param name="response"></param>
		private void IncomingCallCallback(ZoomRoom zoomroom, IncomingCallResponse response)
		{
			IncomingCall incoming = response.IncomingCall;
			if (incoming == null)
				return;

			Parent.Log(eSeverity.Informational, "Incoming call: {0}", incoming.CallerName);
			OnIncomingCall.Raise(this, new GenericEventArgs<IncomingCall>(incoming));
		}

		/// <summary>
		/// Called when the Zoom Room tries to join a meeting that needs a password
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

		private void ListParticipantsCallback(ZoomRoom zoomRoom, ListParticipantsResponse response)
		{
			foreach (ParticipantInfo participant in response.Participants)
				AddUpdateOrRemoveParticipant(participant);
		}

		private void DisconnectCallback(ZoomRoom zoomRoom, CallDisconnectResponse response)
		{
			if (response.Disconnect.Success != eZoomBoolean.on)
				return;

			CallLock = false;
			CallRecord = false;

			ClearParticipants();
		}

		private void CallInfoCallback(ZoomRoom zoomRoom, InfoResultResponse response)
		{
			CallInfo = response.InfoResult;
			if (CallInfo == null)
				return;

			MeetingId = CallInfo.MeetingId;
		}

		private void CallStatusCallback(ZoomRoom zoomRoom, CallStatusResponse response)
		{
			var callStatus = response.CallStatus;
			if (callStatus == null)
				return;

			eCallStatus? status = response.CallStatus.Status;
			Status = status ?? Status;
		}

		private void UpdatedCallRecordInfoCallback(ZoomRoom zoomroom, UpdatedCallRecordInfoResponse response)
		{
			UpdateCallRecordInfoEvent callRecordInfo = response.CallRecordInfo;
			if (callRecordInfo == null)
				return;

			CallRecord = callRecordInfo.AmIRecording;
			OnUpdatedCallRecordInfo.Raise(this, new GenericEventArgs<UpdateCallRecordInfoEvent>(callRecordInfo));
		}

		private void CallRecordStatusCallback(ZoomRoom zoomroom, CallRecordStatusResponse response)
		{
			if (response.Status.State != eZoomRoomResponseState.Error)
				return;

			OnCallRecordErrorState.Raise(this, new StringEventArgs(response.Status.Message));
		}

		private void VideoUnMuteRequestCallback(ZoomRoom zoomroom, VideoUnMuteRequestResponse response)
		{
			OnFarEndRequestedVideoUnMute.Raise(this);
		}

		private void NeedWaitForHostCallback(ZoomRoom zoomroom, NeedWaitForHostResponse response)
		{
			if (response.Response == null)
				return;

			OnNeedWaitForHost.Raise(this, new BoolEventArgs(response.Response.Wait));
		}

		#endregion

		#region Console

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Participants", GetParticipants().Count());
			addRow("MeetingId", MeetingId);
			addRow("CameraMute", CameraMute);
			addRow("MicrophoneMute", MicrophoneMute);
			addRow("AmIHost", AmIHost);
			addRow("CallLock", CallLock);
			addRow("CallRecord", CallRecord);
			addRow("Status", Status);
			addRow("MuteUserOnEntry", MuteUserOnEntry);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("CallLeave", "Leaves the conference", () => CallLeave());
			yield return new ConsoleCommand("CallDisconnect", "Ends the conference", () => CallDisconnect());
			yield return new GenericConsoleCommand<bool>("EnableCallLock", "EnableCallLock <true/false>",
			                                             b => EnableCallLock(b));
			yield return new GenericConsoleCommand<bool>("EnableCallRecord", "EnableCallRecord <true/false>",
			                                             b => EnableCallRecord(b));
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
