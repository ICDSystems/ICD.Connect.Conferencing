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

		#endregion

		private readonly List<ZoomParticipant> m_Participants;
		private readonly SafeCriticalSection m_ParticipantsSection;

		private eConferenceStatus m_Status;

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
			foreach (ParticipantInfo participant in response.Participants)
				AddUpdateOrRemoveParticipant(participant);
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
					ClearParticipants();
					Status = eConferenceStatus.Disconnected;
					break;

				case eCallStatus.UNKNOWN:
					Status = eConferenceStatus.Undefined;
					break;
			}
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
