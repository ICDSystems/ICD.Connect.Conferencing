using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Responses;
using ICD.Connect.Conferencing.Conferences;

namespace ICD.Connect.Conferencing.Zoom.Components.Call
{
	public sealed class CallComponent : AbstractZoomRoomComponent, IWebConference
	{
		private bool m_CameraMute;
		private bool m_MicrophoneMute;
		private string m_Name;
		private eConferenceStatus m_Status;

		private readonly List<ZoomParticipant> m_Participants;
		private readonly SafeCriticalSection m_ParticipantsSection;

		#region Events

		public event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		public event EventHandler<ParticipantEventArgs> OnParticipantAdded;

		public event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		#endregion

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

		public bool CameraMute
		{
			get { return m_CameraMute; }
			set { m_CameraMute = value; }
		}

		public bool MicrophoneMute
		{
			get { return m_MicrophoneMute; }
			set { m_CameraMute = value; }
		}

		public CallInfo CallInfo { get; private set; }

		public eCallType CallType
		{
			get { return eCallType.Video; }
		}

		public bool AmIHost { get; set; }

		#endregion

		#region Constructors

		public CallComponent(ZoomRoom parent) : base(parent)
		{
			Name = "Zoom Meeting";
			m_Participants = new List<ZoomParticipant>();
			m_ParticipantsSection = new SafeCriticalSection();
			Subscribe(Parent);
		}

		protected override void DisposeFinal()
		{
			base.DisposeFinal();

			Unsubscribe(Parent);
		}

		#endregion

		#region Methods

		public IEnumerable<IWebParticipant> GetParticipants()
		{
			return m_ParticipantsSection.Execute(() => m_Participants.Cast<IWebParticipant>().ToList());
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
				return;
			}

			m_ParticipantsSection.Enter();
			try
			{
				if (info.Event == eUserChangedEventType.ZRCUserChangedEventLeftMeeting)
				{
					var remove = m_Participants.Find(p => p.UserId == info.UserId);
					if (remove != null)
						RemoveParticipant(remove);
				}
				else
				{
					var index = m_Participants.FindIndex(p => p.UserId == info.UserId);
					if (index < 0)
						AddParticipant(new ZoomParticipant(Parent, info));
					else
						m_Participants[index].Update(info);
				}

			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		private void AddParticipant(ZoomParticipant participant)
		{
			m_ParticipantsSection.Enter();
			try
			{
				if (m_Participants.Contains(participant))
					return;

				m_Participants.Add(participant);
				OnParticipantAdded.Raise(this, new ParticipantEventArgs(participant));
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		private void RemoveParticipant(ZoomParticipant participant)
		{
			m_ParticipantsSection.Enter();
			try
			{
				if (m_Participants.Remove(participant))
					OnParticipantRemoved.Raise(this, new ParticipantEventArgs(participant));
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		private void ClearParticipants()
		{
			m_ParticipantsSection.Enter();
			try
			{
				foreach (var participant in GetParticipants().Cast<ZoomParticipant>().ToArray())
					RemoveParticipant(participant);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
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
		}

		private void Unsubscribe(ZoomRoom zoomRoom)
		{
			zoomRoom.UnregisterResponseCallback<CallConfigurationResponse>(CallConfigurationCallback);
			zoomRoom.UnregisterResponseCallback<ListParticipantsResponse>(ListParticipantsCallback);
			zoomRoom.UnregisterResponseCallback<CallDisconnectResponse>(DisconnectCallback);
			zoomRoom.UnregisterResponseCallback<InfoResultResponse>(CallInfoCallback);
			zoomRoom.UnregisterResponseCallback<CallStatusResponse>(CallStatusCallback);
		}

		private void CallConfigurationCallback(ZoomRoom room, CallConfigurationResponse response)
		{
			CallConfiguration config = response.CallConfiguration;
			if (config.Microphone != null)
				MicrophoneMute = config.Microphone.Mute;
			if (config.Camera != null)
				CameraMute = config.Camera.Mute;
		}

		private void ListParticipantsCallback(ZoomRoom zoomRoom, ListParticipantsResponse response)
		{
			m_ParticipantsSection.Enter();
			try
			{
				// remove participants that have left
				var participantsToRemove = m_Participants.Where(p => !response.Participants.Any(i => i.UserId == p.UserId)).ToList();
				foreach (var removal in participantsToRemove)
					RemoveParticipant(removal);

				// add or update current participants
				foreach (var info in response.Participants)
					AddUpdateOrRemoveParticipant(info);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}
		}

		private void DisconnectCallback(ZoomRoom zoomRoom, CallDisconnectResponse response)
		{
			if (response.Disconnect.Success == eZoomBoolean.on)
				Status = eConferenceStatus.Disconnected;
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
					Status = eConferenceStatus.Disconnected;
					ClearParticipants();
					break;
				case eCallStatus.UNKNOWN:
					Status = eConferenceStatus.Undefined;
					break;
			}
		}

		#endregion

		#region Console Node

		public override string ConsoleName
		{
			get { return Name; }
		}

		public override string ConsoleHelp
		{
			get { return "Zoom Room Conference"; }
		}

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
		}

		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;
				
			yield return new ConsoleCommand("Leave", "Leaves the conference", () => LeaveConference());
			yield return new ConsoleCommand("End", "Ends the conference", () => EndConference());
			yield return new ConsoleCommand("MuteAll", "Mutes all participants", () => this.MuteAll());
			yield return new ConsoleCommand("KickAll", "Kicks all participants", () => this.KickAll());
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