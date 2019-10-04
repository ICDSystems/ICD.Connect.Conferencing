using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.Zoom.Components.Layout;
using ICD.Connect.Conferencing.Zoom.EventArguments;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	public sealed class CallComponent : AbstractZoomRoomComponent, IWebConference
	{
		#region Events

		/// <summary>
		/// Raised when a participant is removed from the conference.
		/// </summary>
		public event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		/// <summary>
		/// Raised when a participant is added to the conference.
		/// </summary>
		public event EventHandler<ParticipantEventArgs> OnParticipantAdded;

		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		public event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when the call lock status changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCallLockChanged;

		/// <summary>
		/// Raised when the zoom room informs us the call record status has changed.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCallRecordChanged;

		/// <summary>
		/// Raised when the far end sends a video un-mute request.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnVideoUnMuteRequestSent;

		#endregion

		private readonly List<ZoomParticipant> m_Participants;
		private readonly SafeCriticalSection m_ParticipantsSection;

		private eConferenceStatus m_Status;

		private bool m_CallLockEnabled;
		private bool m_CallRecordEnabled;

		#region Properties

		public string Number { get; private set; }

		public string Name { get; private set; }

		public eConferenceStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (value == m_Status)
					return;

				m_Status = value;
				Parent.Log(eSeverity.Informational, "Call {0} status changed: {1}", Number, StringUtils.NiceName(m_Status));

				OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(m_Status));
			}
		}

		public DateTime? Start { get; private set; }

		public DateTime? End { get; private set; }

		public bool CameraMute { get; private set; }

		public bool MicrophoneMute { get; private set; }

		public CallInfo CallInfo { get; private set; }

		public eCallType CallType { get { return eCallType.Video; } }

		public bool AmIHost { get; private set; }

		/// <summary>
		/// Gets the CallLock State.
		/// </summary>
		public bool CallLock
		{
			get { return m_CallLockEnabled; }
			private set
			{
				if (value == m_CallLockEnabled)
					return;

				m_CallLockEnabled = value;

				Parent.Log(eSeverity.Informational, "CallLock set to {0}", m_CallLockEnabled);

				OnCallLockChanged.Raise(this, new BoolEventArgs(m_CallLockEnabled));
			}
		}

		/// <summary>
		/// Gets the Call Record State.
		/// </summary>
		public bool CallRecord
		{
			get { return m_CallRecordEnabled; }
			private set
			{
				if (value == m_CallRecordEnabled)
					return;

				m_CallRecordEnabled = value;

				Parent.Log(eSeverity.Informational, "Call Record set to {0}", m_CallRecordEnabled);

				OnCallRecordChanged.Raise(this, new BoolEventArgs(m_CallRecordEnabled));
			}
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="parent"></param>
		public CallComponent(ZoomRoom parent)
			: base(parent)
		{
			Name = "Zoom Meeting";
			m_Participants = new List<ZoomParticipant>();
			m_ParticipantsSection = new SafeCriticalSection();
			Subscribe(Parent);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeFinal()
		{
			base.DisposeFinal();

			OnParticipantRemoved = null;
			OnParticipantAdded = null;
			OnStatusChanged = null;
			OnCallLockChanged = null;
			OnCallRecordChanged = null;
			OnVideoUnMuteRequestSent = null;

			Unsubscribe(Parent);
		}

		#endregion

		#region Methods

		public IEnumerable<IWebParticipant> GetParticipants()
		{
			return m_ParticipantsSection.Execute(() => m_Participants.Cast<IWebParticipant>().ToArray());
		}

		IEnumerable<IParticipant> IConference.GetParticipants()
		{
			return GetParticipants().Cast<IParticipant>();
		}

		public void LeaveConference()
		{
			Parent.Log(eSeverity.Debug, "Leaving Zoom Meeting {0}", Number);
			Status = eConferenceStatus.Disconnecting;
			Parent.SendCommand("zCommand Call Leave");
		}

		public void EndConference()
		{
			Parent.Log(eSeverity.Debug, "Ending Zoom Meeting {0}", Number);
			Status = eConferenceStatus.Disconnecting;
			Parent.SendCommand("zCommand Call Disconnect");
		}

		public void SetCallLock(bool enabled)
		{
			Parent.Log(eSeverity.Debug, "Setting the Call Lock state to: {0}", enabled);
			Parent.SendCommand("zConfiguration Call Lock Enable: {0}", enabled ? "on" : "off");
		}

		public void SetCallRecord(bool enabled)
		{
			Parent.Log(eSeverity.Debug, "Setting the Call Record state to: {0}", enabled);
			Parent.SendCommand("zCommand Call Record Enable: {0}", enabled ? "on" : "off");
		}

		public void SetCallCameraMute(bool enabled)
		{
			Parent.Log(eSeverity.Debug, "Setting the Call Camera Mute state to: {0}", enabled);
			Parent.SendCommand("zConfiguration Call Camera Mute: {0}", enabled ? "on" : "off");
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

		private void AddUpdateOrRemoveParticipant(ParticipantInfo info)
		{
			if (info.IsMyself)
			{
				AmIHost = info.IsHost || info.IsCohost;
				OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(Status));
			}

			switch (info.Event)
			{
				case eUserChangedEventType.ZRCUserChangedEventLeftMeeting:
					RemoveParticipant(info);
					break;

				case eUserChangedEventType.None:
				case eUserChangedEventType.ZRCUserChangedEventJoinedMeeting:
				case eUserChangedEventType.ZRCUserChangedEventUserInfoUpdated:
					AddOrUpdateParticipant(info);
					break;

				case eUserChangedEventType.ZRCUserChangedEventHostChanged:
					SetNewHost(info);
					OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(Status));
					break;
			}
		}

		private void AddOrUpdateParticipant(ParticipantInfo info)
		{
			ZoomParticipant participant;

			m_ParticipantsSection.Enter();

			try
			{
				participant = m_Participants.Find(p => p.UserId == info.UserId);
				if (participant != null)
					participant.Update(info);
				else
				{
					participant = new ZoomParticipant(Parent, info);
					m_Participants.Add(participant);
				}
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}

			OnParticipantAdded.Raise(this, new ParticipantEventArgs(participant));
		}

		private void RemoveParticipant(ParticipantInfo info)
		{
			ZoomParticipant participant = m_ParticipantsSection.Execute(() => m_Participants.Find(p => p.UserId == info.UserId));
			RemoveParticipant(participant);
		}

		private void RemoveParticipant(ZoomParticipant participant)
		{
			if (participant == null)
				return;

			if (!m_ParticipantsSection.Execute(() => m_Participants.Remove(participant)))
				return;

			OnParticipantRemoved.Raise(this, new ParticipantEventArgs(participant));
		}

		private void ClearParticipants()
		{
			foreach (var participant in m_ParticipantsSection.Execute(() => m_Participants.ToArray()))
				RemoveParticipant(participant);
		}

		private void SetNewHost(ParticipantInfo info)
		{
			foreach (var participant in m_ParticipantsSection.Execute(() => m_Participants.ToArray()))
				participant.SetIsHost(participant.UserId == info.UserId);
		}

		#endregion

		#region Zoom Room Callbacks

		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
			zoomRoom.RegisterResponseCallback<ListParticipantsResponse>(ListParticipantsCallback);
			zoomRoom.RegisterResponseCallback<CallDisconnectResponse>(DisconnectCallback);
			zoomRoom.RegisterResponseCallback<InfoResultResponse>(CallInfoCallback);
			zoomRoom.RegisterResponseCallback<CallStatusResponse>(CallStatusCallback);
			zoomRoom.RegisterResponseCallback<UpdatedCallRecordInfoResponse>(UpdatedCallRecordInfoCallback);
			zoomRoom.RegisterResponseCallback<VideoUnMuteRequestResponse>(VideoUnMuteRequestCallback);
		}

		private void Unsubscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.UnregisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
			zoomRoom.UnregisterResponseCallback<ListParticipantsResponse>(ListParticipantsCallback);
			zoomRoom.UnregisterResponseCallback<CallDisconnectResponse>(DisconnectCallback);
			zoomRoom.UnregisterResponseCallback<InfoResultResponse>(CallInfoCallback);
			zoomRoom.UnregisterResponseCallback<CallStatusResponse>(CallStatusCallback);
			zoomRoom.UnregisterResponseCallback<UpdatedCallRecordInfoResponse>(UpdatedCallRecordInfoCallback);
			zoomRoom.UnregisterResponseCallback<VideoUnMuteRequestResponse>(VideoUnMuteRequestCallback);
		}

		private void CallConfigurationCallback(ZoomRoom room, CallConfigurationResponse response)
		{
			CallConfiguration config = response.CallConfiguration;

			if (config.Microphone != null)
				MicrophoneMute = config.Microphone.Mute;

			if (config.Camera != null)
				CameraMute = config.Camera.Mute;

			if (config.CallLockStatus != null)
				CallLock = config.CallLockStatus.Lock;
		}

		private void ListParticipantsCallback(ZoomRoom zoomRoom, ListParticipantsResponse response)
		{
			foreach (ParticipantInfo participant in response.Participants)
				AddUpdateOrRemoveParticipant(participant);
		}

		private void DisconnectCallback(ZoomRoom zoomRoom, CallDisconnectResponse response)
		{
			if (response.Disconnect.Success == eZoomBoolean.on)
			{
				Status = eConferenceStatus.Disconnected;
				CallLock = false;
				CallRecord = false;
			}
		}

		private void CallInfoCallback(ZoomRoom zoomRoom, InfoResultResponse response)
		{
			CallInfo result = response.InfoResult;
			CallInfo = response.InfoResult;
			Number = result.MeetingId;

			OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(Status));
		}

		private void CallStatusCallback(ZoomRoom zoomRoom, CallStatusResponse response)
		{
			var status = response.CallStatus.Status;
			switch (status)
			{
				case eCallStatus.CONNECTING_MEETING:
					Status = eConferenceStatus.Connecting;
					break;

				case eCallStatus.IN_MEETING:
					Status = eConferenceStatus.Connected;
					Start = IcdEnvironment.GetLocalTime();
					break;

				case eCallStatus.NOT_IN_MEETING:
				case eCallStatus.LOGGED_OUT:
					ClearParticipants();
					Status = eConferenceStatus.Disconnected;
					break;

				case eCallStatus.UNKNOWN:
					Status = eConferenceStatus.Undefined;
					break;
			}
		}

		private void UpdatedCallRecordInfoCallback(ZoomRoom zoomroom, UpdatedCallRecordInfoResponse response)
		{
			var callRecordInfo = response.CallRecordInfo;
			CallRecord = callRecordInfo.AmIRecording;
		}

		private void VideoUnMuteRequestCallback(ZoomRoom zoomroom, VideoUnMuteRequestResponse response)
		{
			OnVideoUnMuteRequestSent.Raise(this, new BoolEventArgs(true));
		}

		#endregion

		#region Console

		public override string ConsoleName { get { return Name; } }

		public override string ConsoleHelp { get { return "Zoom Room Conference"; } }

		public override IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			foreach (IConsoleNodeBase node in GetBaseConsoleNodes())
				yield return node;

			foreach (var participant in GetParticipants())
				yield return participant;
		}

		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("Name", Name);
			addRow("Number", Number);
			addRow("Status", Status);
			addRow("Participants", GetParticipants().Count());
			addRow("CallLock", CallLock);
			addRow("CallRecord", CallRecord);
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new ConsoleCommand("Leave", "Leaves the conference", () => LeaveConference());
			yield return new ConsoleCommand("End", "Ends the conference", () => EndConference());
			yield return new ConsoleCommand("MuteAll", "Mutes all participants", () => this.MuteAll());
			yield return new ConsoleCommand("KickAll", "Kicks all participants", () => this.KickAll());
			yield return new GenericConsoleCommand<bool>("SetCallLock", "SetCallLock <true/false>",
			                                             b => SetCallLock(b));
			yield return new GenericConsoleCommand<bool>("SetCallRecord", "SetCallRecord <true/false>",
			                                             b => SetCallRecord(b));
		}

		private IEnumerable<IConsoleNodeBase> GetBaseConsoleNodes()
		{
			return base.GetConsoleNodes();
		}

		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
