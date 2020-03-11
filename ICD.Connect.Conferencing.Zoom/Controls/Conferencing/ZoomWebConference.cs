using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;
using ICD.Connect.Conferencing.Zoom.Components.Call;
using ICD.Connect.Conferencing.Zoom.Responses;

namespace ICD.Connect.Conferencing.Zoom.Controls.Conferencing
{
	public sealed class ZoomWebConference : IWebConference
	{
		/// <summary>
		/// Raised when a participant is added to the conference.
		/// </summary>
		public event EventHandler<ParticipantEventArgs> OnParticipantAdded;

		/// <summary>
		/// Raised when a participant is removed from the conference.
		/// </summary>
		public event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		public event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		private readonly IcdOrderedDictionary<string, ZoomWebParticipant> m_Participants;
		private readonly SafeCriticalSection m_ParticipantsSection;

		private readonly CallComponent m_CallComponent;
		private eConferenceStatus m_Status;

		#region Properties

		/// <summary>
		/// Current conference status.
		/// </summary>
		public eConferenceStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (value == m_Status)
					return;

				m_Status = value;

				OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(m_Status));
			}
		}

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		public DateTime? Start { get; private set; }

		/// <summary>
		/// The time the call ended.
		/// </summary>
		public DateTime? End { get; private set; }

		/// <summary>
		/// Gets the type of call.
		/// </summary>
		public eCallType CallType { get { return eCallType.Video; } }

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="callComponent"></param>
		public ZoomWebConference(CallComponent callComponent)
		{
			if (callComponent == null)
				throw new ArgumentNullException("callComponent");

			m_Participants = new IcdOrderedDictionary<string, ZoomWebParticipant>();
			m_ParticipantsSection = new SafeCriticalSection();

			m_CallComponent = callComponent;
			Subscribe(m_CallComponent);
		}

		#region Methods

		/// <summary>
		/// Leaves the conference, keeping the conference in tact for other participants.
		/// </summary>
		public void LeaveConference()
		{
			Status = eConferenceStatus.Disconnecting;
			m_CallComponent.CallLeave();
		}

		/// <summary>
		/// Ends the conference for all participants.
		/// </summary>
		public void EndConference()
		{
			Status = eConferenceStatus.Disconnecting;
			m_CallComponent.CallDisconnect();
		}

		/// <summary>
		/// Gets the sources in this conference.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IWebParticipant> GetParticipants()
		{
			return m_ParticipantsSection.Execute(() => m_Participants.Values.ToArray());
		}

		/// <summary>
		/// Gets the sources in this conference.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IParticipant> IConference.GetParticipants()
		{
			return GetParticipants().Cast<IParticipant>();
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Adds the participant to the conference.
		/// </summary>
		/// <param name="participant"></param>
		private void AddParticipant([NotNull] ZoomWebParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			m_ParticipantsSection.Execute(() => m_Participants.Add(participant.UserId, participant));

			OnParticipantAdded.Raise(this, new ParticipantEventArgs(participant));
		}

		/// <summary>
		/// Removes the participant from the conference.
		/// </summary>
		/// <param name="participant"></param>
		private void RemoveParticipant([NotNull] ZoomWebParticipant participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			if (m_ParticipantsSection.Execute(() => m_Participants.Remove(participant.UserId)))
				OnParticipantRemoved.Raise(this, new ParticipantEventArgs(participant));
		}

		/// <summary>
		/// Clears all participants from the conference.
		/// </summary>
		private void Clear()
		{
			foreach (ZoomWebParticipant participant in GetParticipants().Cast<ZoomWebParticipant>())
				RemoveParticipant(participant);
		}

		#endregion

		#region CallComponent Callbacks

		/// <summary>
		/// Subscribe to the call component events.
		/// </summary>
		/// <param name="callComponent"></param>
		private void Subscribe(CallComponent callComponent)
		{
			callComponent.OnStatusChanged += CallComponentOnStatusChanged;
			callComponent.OnParticipantAdded += CallComponentOnParticipantAdded;
			callComponent.OnParticipantUpdated += CallComponentOnParticipantUpdated;
			callComponent.OnParticipantRemoved += CallComponentOnParticipantRemoved;
			callComponent.OnNeedWaitForHost += CallComponentOnOnNeedWaitForHost;
		}

		/// <summary>
		/// Called when a participant is added to the room.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnParticipantRemoved(object sender, GenericEventArgs<ParticipantInfo> eventArgs)
		{
			ZoomWebParticipant participant;

			m_ParticipantsSection.Enter();

			try
			{
				if (!m_Participants.TryGetValue(eventArgs.Data.UserId, out participant))
					return;

				participant.Update(eventArgs.Data);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}

			RemoveParticipant(participant);
		}

		/// <summary>
		/// Called when a participant is added to the room.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnParticipantAdded(object sender, GenericEventArgs<ParticipantInfo> eventArgs)
		{
			ZoomWebParticipant participant =
				m_ParticipantsSection.Execute(() => m_Participants.GetDefault(eventArgs.Data.UserId));

			if (participant == null)
			{
				participant = new ZoomWebParticipant(m_CallComponent, eventArgs.Data);
				AddParticipant(participant);
			}
			else
				participant.Update(eventArgs.Data);
		}

		/// <summary>
		/// Called when a participant is updated in the room.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnParticipantUpdated(object sender, GenericEventArgs<ParticipantInfo> eventArgs)
		{
			ZoomWebParticipant participant =
				m_ParticipantsSection.Execute(() => m_Participants.GetDefault(eventArgs.Data.UserId));

			if (participant == null)
			{
				participant = new ZoomWebParticipant(m_CallComponent, eventArgs.Data);
				AddParticipant(participant);
			}
			else
				participant.Update(eventArgs.Data);
		}

		/// <summary>
		/// Called when the conference status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="eventArgs"></param>
		private void CallComponentOnStatusChanged(object sender, GenericEventArgs<eCallStatus> eventArgs)
		{
			switch (eventArgs.Data)
			{
				case eCallStatus.CONNECTING_MEETING:
					Status = eConferenceStatus.Connecting;
					break;

				case eCallStatus.IN_MEETING:
					Status = eConferenceStatus.Connected;
					Start = IcdEnvironment.GetUtcTime();
					break;

				case eCallStatus.NOT_IN_MEETING:
				case eCallStatus.LOGGED_OUT:
					Clear();
					Status = eConferenceStatus.Disconnected;
					break;

				case eCallStatus.UNKNOWN:
					Status = eConferenceStatus.Undefined;
					break;
			}
		}

		/// <summary>
		/// Zoom doesn't give connection feedback upon joining a call lobby,
		/// but we do get feedback when we need to wait for the host to start the call.
		/// So when we are waiting for the host in the lobby change the conference status to connected.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void CallComponentOnOnNeedWaitForHost(object sender, BoolEventArgs e)
		{
			if (e.Data)
				Status = eConferenceStatus.Connected;
		}

		#endregion

		#region Console

		public string ConsoleName { get { return GetType().Name; } }

		public string ConsoleHelp { get { return "Zoom Room Conference"; } }

		public IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			return GetParticipants().Cast<IConsoleNodeBase>();
		}

		public void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Status", Status);
			addRow("Participants", GetParticipants().Count());
		}

		public IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Leave", "Leaves the conference", () => LeaveConference());
			yield return new ConsoleCommand("End", "Ends the conference", () => EndConference());
			yield return new ConsoleCommand("MuteAll", "Mutes all participants", () => this.MuteAll());
			yield return new ConsoleCommand("KickAll", "Kicks all participants", () => this.KickAll());
		}

		#endregion
	}
}
