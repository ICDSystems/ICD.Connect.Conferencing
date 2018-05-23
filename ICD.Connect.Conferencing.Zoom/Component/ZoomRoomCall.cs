using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Cameras;
using ICD.Connect.Conferencing.ConferenceSources;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Component
{
	public sealed class ZoomRoomCall : IConferenceSource
	{
		private bool m_CameraMute;
		private bool m_MicrophoneMute;
		private string m_Name;
		private eConferenceSourceStatus m_Status;

		#region Events

		public event EventHandler<ConferenceSourceAnswerStateEventArgs> OnAnswerStateChanged;

		public event EventHandler<ConferenceSourceStatusEventArgs> OnStatusChanged;

		public event EventHandler<StringEventArgs> OnNameChanged;

		public event EventHandler<StringEventArgs> OnNumberChanged;

		public event EventHandler<ConferenceSourceTypeEventArgs> OnSourceTypeChanged;

		#endregion

		#region Properties

		public string Name
		{
			get { return m_Name; }
			private set
			{
				if (value == m_Name)
					return;

				m_Name = value;

				OnNameChanged.Raise(this, new StringEventArgs(m_Name));
			}
		}

		public string Number { get; private set; }

		public eConferenceSourceType SourceType
		{
			get { return eConferenceSourceType.Video; }
		}

		public eConferenceSourceStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (value == m_Status)
					return;

				m_Status = value;
				ZoomRoom.Log(eSeverity.Informational, "Call {0} status changed: {1}", Number, StringUtils.NiceName(m_Status));

				OnStatusChanged.Raise(this, new ConferenceSourceStatusEventArgs(m_Status));
			}
		}

		public eConferenceSourceDirection Direction { get; private set; }

		public eConferenceSourceAnswerState AnswerState { get; private set; }

		public DateTime? Start { get; private set; }
		
		public DateTime? End { get; private set; }

		public DateTime DialTime { get; private set; }

		public DateTime StartOrDialTime
		{
			get { return Start ?? DialTime; }
		}

		public IRemoteCamera Camera
		{
			get { return null; }
		}

		public string CallerJoinId { get; private set; }

		public ZoomRoom ZoomRoom { get; private set; }

		public bool CameraMute
		{
			get { return m_CameraMute; }
			set
			{
				m_CameraMute = value;
				UpdateSourceHoldStatus();
			}
		}

		public bool MicrophoneMute
		{
			get { return m_MicrophoneMute; }
			set
			{
				m_CameraMute = value;
				UpdateSourceHoldStatus();
			}
		}

		public CallInfo CallInfo { get; private set; }

		public List<ParticipantInfo> Participants { get; private set; }

		#endregion

		#region Constructors

		public ZoomRoomCall(IncomingCall call, ZoomRoom zoomRoom)
		{
			ZoomRoom = zoomRoom;

			CallerJoinId = call.CallerJoinId;
			Start = IcdEnvironment.GetLocalTime();
			Name = call.CallerName;
			Number = call.MeetingNumber.ToString();
			Direction = eConferenceSourceDirection.Incoming;
			Participants = new List<ParticipantInfo>();
			Status = eConferenceSourceStatus.Ringing;

			Subscribe(ZoomRoom);
		}

		public ZoomRoomCall(ZoomRoom zoomRoom)
		{
			Direction = eConferenceSourceDirection.Outgoing;
			ZoomRoom = zoomRoom;
		}

		#endregion

		#region Methods

		public void Answer()
		{
			ZoomRoom.SendCommand("zCommand Call Accept callerJid: {0}", CallerJoinId);
		}

		public void Hold()
		{
			if (Status == eConferenceSourceStatus.OnHold)
				return;
			ZoomRoom.SendCommand("zConfiguration Call Microphone mute: on");
			ZoomRoom.SendCommand("zConfiguration Call Camera mute: on");
		}

		public void Resume()
		{
			if (Status != eConferenceSourceStatus.OnHold)
				return;
			ZoomRoom.SendCommand("zConfiguration Call Microphone mute: off");
			ZoomRoom.SendCommand("zConfiguration Call Camera mute: off");
		}

		public void Hangup()
		{
			ZoomRoom.SendCommand("zCommand Call Disconnect");
			Status = eConferenceSourceStatus.Disconnecting;
			ZoomRoom.Log(eSeverity.Debug, "Disconnecting call {0}", Name);
		}

		public void SendDtmf(string data)
		{
			ZoomRoom.Log(eSeverity.Warning, "Zoom Room does not support sending DTMF");
		}

		#endregion

		#region Private Methods

		private void UpdateSourceHoldStatus()
		{
			if (Status == eConferenceSourceStatus.Connected && CameraMute && MicrophoneMute)
				Status = eConferenceSourceStatus.OnHold;

			else if (Status == eConferenceSourceStatus.OnHold && !(CameraMute && MicrophoneMute))
				Status = eConferenceSourceStatus.Connected;
		}

		#endregion

		#region Zoom Room Callbacks

		private void Subscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.RegisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
			zoomRoom.RegisterResponseCallback<ParticipantUpdateResponse>(ParticipantUpdateCallback);
			zoomRoom.RegisterResponseCallback<CallDisconnectResponse>(DisconnectCallback);
			zoomRoom.RegisterResponseCallback<InfoResultResponse>(CallInfoCallback);
		}

		private void Unsubscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.UnregisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
			zoomRoom.UnregisterResponseCallback<ParticipantUpdateResponse>(ParticipantUpdateCallback);
			zoomRoom.UnregisterResponseCallback<CallDisconnectResponse>(DisconnectCallback);
		}

		private void CallConfigurationCallback(ZoomRoom room, CallConfigurationResponse response)
		{
			CallConfiguration config = response.CallConfiguration;
			if (config.Microphone != null)
				MicrophoneMute = config.Microphone.Mute;
			if (config.Camera != null)
				CameraMute = config.Camera.Mute;
		}

		private void ParticipantUpdateCallback(ZoomRoom zoomRoom, ParticipantUpdateResponse response)
		{
			int index = Participants.FindIndex(p => p.UserId == response.Participant.UserId);
			if (index >= 0)
				Participants[index] = response.Participant;
			else
				Participants.Add(response.Participant);
		}

		private void DisconnectCallback(ZoomRoom zoomRoom, CallDisconnectResponse response)
		{
			if (response.Disconnect.Success == eZoomBoolean.on)
				Status = eConferenceSourceStatus.Disconnected;
		}

		private void CallInfoCallback(ZoomRoom zoomRoom, InfoResultResponse response)
		{
			CallInfo result = response.InfoResult;
		}

		#endregion

		#region IConsoleNode Members

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IConsoleNodeBase Members

		public string ConsoleName
		{
			get { throw new NotImplementedException(); }
		}

		public string ConsoleHelp
		{
			get { throw new NotImplementedException(); }
		}

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}