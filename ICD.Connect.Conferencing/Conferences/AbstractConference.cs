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
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public abstract class AbstractConference<T> : IConference<T>
		where T: class, IParticipant
	{
		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		public event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Raised when a participant is added to the conference.
		/// </summary>
		public event EventHandler<ParticipantEventArgs> OnParticipantAdded;

		/// <summary>
		/// Raised when a participant is removed from the conference.
		/// </summary>
		public event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		/// <summary>
		/// Raised when the start time changes
		/// </summary>
		public event EventHandler<DateTimeNullableEventArgs> OnStartTimeChanged;

		/// <summary>
		/// Raised when the end time changes
		/// </summary>
		public event EventHandler<DateTimeNullableEventArgs> OnEndTimeChanged;

		/// <summary>
		/// Raised when the conference's call type changes.
		/// </summary>
		public event EventHandler<GenericEventArgs<eCallType>> OnCallTypeChanged;

		/// <summary>
		/// Raised when the can record state changes.
		/// </summary>
		public event EventHandler<BoolEventArgs> OnCanRecordChanged;

		/// <summary>
		/// Raised when the conference's recording status changes.
		/// </summary>
		public event EventHandler<ConferenceRecordingStatusEventArgs> OnConferenceRecordingStatusChanged;

		/// <summary>
		/// Raised when the supported conference features changes.
		/// </summary>
		public event EventHandler<GenericEventArgs<eConferenceFeatures>> OnSupportedConferenceFeaturesChanged;

		private readonly IcdHashSet<T> m_Participants;
		private readonly SafeCriticalSection m_ParticipantsSection;

		private eConferenceStatus m_Status;
		private DateTime? m_Start;
		private DateTime? m_End;
		private bool m_CanRecord;
		private eConferenceRecordingStatus m_RecordingStatus;
		private eConferenceFeatures m_SupportedConferenceFeatures;

		/// <summary>
		/// Maps participant status to conference status.
		/// </summary>
// ReSharper disable StaticFieldInGenericType
		private static readonly Dictionary<eParticipantStatus, eConferenceStatus> s_StatusMap =
// ReSharper restore StaticFieldInGenericType
			new Dictionary<eParticipantStatus, eConferenceStatus>
			{
				{eParticipantStatus.Undefined, eConferenceStatus.Undefined},
				{eParticipantStatus.Dialing, eConferenceStatus.Connecting},
				{eParticipantStatus.Ringing, eConferenceStatus.Connecting},
				{eParticipantStatus.Invited, eConferenceStatus.Connecting},
				{eParticipantStatus.Connecting, eConferenceStatus.Connecting},
				{eParticipantStatus.Waiting, eConferenceStatus.Connected},
				{eParticipantStatus.Connected, eConferenceStatus.Connected},
				{eParticipantStatus.EarlyMedia, eConferenceStatus.Connected},
				{eParticipantStatus.Preserved, eConferenceStatus.Connected},
				{eParticipantStatus.RemotePreserved, eConferenceStatus.Connected},
				{eParticipantStatus.OnHold, eConferenceStatus.OnHold},
				{eParticipantStatus.Disconnecting, eConferenceStatus.Disconnected},
				{eParticipantStatus.Idle, eConferenceStatus.Disconnected},
				{eParticipantStatus.Disconnected, eConferenceStatus.Disconnected},
			};

		#region Properties

		/// <summary>
		/// Current conference status.
		/// </summary>
		public eConferenceStatus Status
		{
			get { return m_Status; }
			protected set
			{
				if(m_Status == value)
					return;

				m_Status = value;

				OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(value));
			}
		}

		/// <summary>
		/// The time the conference started.
		/// </summary>
		public DateTime? StartTime
		{
			get { return m_Start; }
			protected set
			{
				if (m_Start == value)
					return;

				m_Start = value;

				OnStartTimeChanged.Raise(this, new DateTimeNullableEventArgs(value));
			}
		}

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		public DateTime? EndTime
		{
			get { return m_End; }
			protected set
			{
				if (m_End == value)
					return;

				m_End = value;

				OnEndTimeChanged.Raise(this, new DateTimeNullableEventArgs(value));
			}
		}

		/// <summary>
		/// Gets the type of call.
		/// </summary>
		public eCallType CallType
		{
			get { return this.GetOnlineParticipants().MaxOrDefault(p => p.CallType); }
		}

		/// <summary>
		/// Whether or not the the conference can be recorded by the control system.
		/// </summary>
		public bool CanRecord
		{
			get { return m_CanRecord; }
			protected set
			{
				if (m_CanRecord == value)
					return;

				m_CanRecord = value;

				OnCanRecordChanged.Raise(this, m_CanRecord);
			}
		}

		/// <summary>
		/// Gets the status of the conference recording.
		/// </summary>
		public eConferenceRecordingStatus RecordingStatus
		{
			get { return m_RecordingStatus; }
			protected set
			{
				if (m_RecordingStatus == value)
					return;

				m_RecordingStatus = value;

				OnConferenceRecordingStatusChanged.Raise(this, new ConferenceRecordingStatusEventArgs(m_RecordingStatus));
			}
		}

		/// <summary>
		/// Gets the supported conference features.
		/// </summary>
		public eConferenceFeatures SupportedConferenceFeatures
		{
			get { return m_SupportedConferenceFeatures; }
			protected set
			{
				if (m_SupportedConferenceFeatures == value)
					return;

				m_SupportedConferenceFeatures = value;

				OnSupportedConferenceFeaturesChanged.Raise(this, new GenericEventArgs<eConferenceFeatures>(m_SupportedConferenceFeatures));
			}
		}

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractConference()
		{
			m_Participants = new IcdHashSet<T>();
			m_ParticipantsSection = new SafeCriticalSection();
		}

		#region Methods

		/// <summary>
		/// Clears the sources from the conference.
		/// </summary>
		public void Clear()
		{
			foreach (T participant in GetParticipants())
				RemoveParticipant(participant);
		}

		/// <summary>
		/// Adds the participant to the conference.
		/// </summary>
		/// <param name="participant"></param>
		/// <returns>False if the participant is already in the conference.</returns>
		public bool AddParticipant([NotNull] T participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			m_ParticipantsSection.Enter();

			try
			{
				if (!m_Participants.Add(participant))
					return false;

				Subscribe(participant);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}

			UpdateStatus();

			OnParticipantAdded.Raise(this, new ParticipantEventArgs(participant));

			return true;
		}

		/// <summary>
		/// Removes the participant from the conference.
		/// </summary>
		/// <param name="participant"></param>
		/// <returns>False if the participant is not in the conference.</returns>
		public bool RemoveParticipant([NotNull] T participant)
		{
			if (participant == null)
				throw new ArgumentNullException("participant");

			m_ParticipantsSection.Enter();

			try
			{
				if (!m_Participants.Remove(participant))
					return false;

				Unsubscribe(participant);
			}
			finally
			{
				m_ParticipantsSection.Leave();
			}

			UpdateStatus();

			OnParticipantRemoved.Raise(this, new ParticipantEventArgs(participant));

			return true;
		}

		/// <summary>
		/// Gets the participants in this conference.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<T> GetParticipants()
		{
			return m_ParticipantsSection.Execute(() => m_Participants.ToArray());
		}

		/// <summary>
		/// Leaves the conference, keeping the conference in tact for other participants.
		/// </summary>
		public abstract void LeaveConference();

		/// <summary>
		/// Ends the conference for all participants.
		/// </summary>
		public abstract void EndConference();

		/// <summary>
		/// Starts recording the conference.
		/// </summary>
		public abstract void StartRecordingConference();

		/// <summary>
		/// Stops recording the conference.
		/// </summary>
		public abstract void StopRecordingConference();

		/// <summary>
		/// Pauses the current recording of the conference.
		/// </summary>
		public abstract void PauseRecordingConference();

		/// <summary>
		/// Gets the sources in this conference.
		/// </summary>
		/// <returns></returns>
		IEnumerable<IParticipant> IConference.GetParticipants()
		{
			return GetParticipants() as IEnumerable<IParticipant>;
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		public void Dispose()
		{
			OnStatusChanged = null;
			OnParticipantAdded = null;
			OnParticipantRemoved = null;

			Clear();

			DisposeFinal();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected virtual void DisposeFinal()
		{
		}

		#endregion

		#region Private Methods

		private void UpdateStatus()
		{
			Status = GetStatusFromSources();
			UpdateStartAndEndTime();
		}

		private eConferenceStatus GetStatusFromSources()
		{
			IcdHashSet<eConferenceStatus> statuses =
				GetParticipants().Select(s => s_StatusMap[s.Status])
				                 .ToIcdHashSet();

			// All participants left the conference
			if (statuses.Count == 0)
				return eConferenceStatus.Disconnected;

			// All statuses are the same
			if (statuses.Count == 1)
				return statuses.First();

			// If someone is connected then the conference is connected.
			if (statuses.Contains(eConferenceStatus.Connected))
				return eConferenceStatus.Connected;

			// If someone is on hold everyone else is on hold (or connecting)
			if (statuses.Contains(eConferenceStatus.OnHold))
				return eConferenceStatus.OnHold;
			if (statuses.Contains(eConferenceStatus.Connecting))
				return eConferenceStatus.Connecting;

			// If we don't know the current state, we shouldn't assume we've disconnected.
			return eConferenceStatus.Undefined;
		}

		private void UpdateStartAndEndTime()
		{
			UpdateStartTime();
			UpdateEndTime();
		}

		private void UpdateStartTime()
		{
			DateTime? start;
			GetParticipants().Select(s => s.StartTime)
			                 .Where(s => s != null)
			                 .Order()
			                 .TryFirst(out start);
			if (start != null)
				StartTime = start;
		}

		private void UpdateEndTime()
		{
			DateTime? end;
			GetParticipants().Select(e => e.EndTime)
			                 .Where(e => e != null)
			                 .Order()
			                 .TryFirst(out end);
			if (end != null)
				EndTime = end;
		}

		#endregion

		#region Participant Callbacks

		/// <summary>
		/// Subscribes to the participant events.
		/// </summary>
		/// <param name="participant"></param>
		private void Subscribe(IParticipant participant)
		{
			participant.OnStatusChanged += ParticipantOnStatusChanged;
			participant.OnStartTimeChanged += ParticipantOnStartTimeChanged;
			participant.OnEndTimeChanged += ParticipantOnEndTimeChanged;
			participant.OnParticipantTypeChanged += ParticipantOnParticipantTypeChanged;
		}

		/// <summary>
		/// Unsubscribes from the participant events.
		/// </summary>
		/// <param name="participant"></param>
		private void Unsubscribe(IParticipant participant)
		{
			participant.OnStatusChanged -= ParticipantOnStatusChanged;
			participant.OnStartTimeChanged -= ParticipantOnStartTimeChanged;
			participant.OnEndTimeChanged -= ParticipantOnEndTimeChanged;
			participant.OnParticipantTypeChanged -= ParticipantOnParticipantTypeChanged;
		}

		/// <summary>
		/// Called when a participant status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ParticipantOnStatusChanged(object sender, ParticipantStatusEventArgs args)
		{
			UpdateStatus();
		}

		/// <summary>
		/// Called when a participant start time changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ParticipantOnStartTimeChanged(object sender, DateTimeNullableEventArgs args)
		{
			UpdateStartTime();
		}

		/// <summary>
		/// Called when a participant end time changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ParticipantOnEndTimeChanged(object sender, DateTimeNullableEventArgs args)
		{
			UpdateEndTime();
		}

		/// <summary>
		/// Called when a participant call type changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void ParticipantOnParticipantTypeChanged(object sender, CallTypeEventArgs args)
		{
			OnCallTypeChanged.Raise(this, new GenericEventArgs<eCallType>(args.Data));
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the name of the node.
		/// </summary>
		public virtual string ConsoleName { get { return GetType().Name; } }

		/// <summary>
		/// Gets the help information for the node.
		/// </summary>
		public virtual string ConsoleHelp { get { return string.Empty; }  }

		/// <summary>
		/// Gets the child console nodes.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return ConsoleNodeGroup.IndexNodeMap("Participants", "The collection of participants in this conference", GetParticipants());
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Status", Status);
			addRow("StartTime", StartTime);
			addRow("EndTime", EndTime);
			addRow("CallType", CallType);
			addRow("ParticipantCount", GetParticipants().Count());
		}

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield return new ConsoleCommand("Leave", "Leaves the conference", () => LeaveConference());
			yield return new ConsoleCommand("End", "Ends the conference", () => EndConference());
			yield return new ConsoleCommand("MuteAll", "Mutes all participants", () => this.MuteAll());
			yield return new ConsoleCommand("KickAll", "Kicks all participants", () => this.KickAll());
		}

		#endregion
	}
}