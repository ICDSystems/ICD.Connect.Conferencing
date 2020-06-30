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

		private readonly IcdHashSet<T> m_Participants;
		private readonly SafeCriticalSection m_ParticipantsSection;

		private eConferenceStatus m_Status;

		private DateTime? m_Start;
		private DateTime? m_End;

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
				{eParticipantStatus.Connecting, eConferenceStatus.Connecting},
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

		public eConferenceStatus Status
		{
			get { return m_Status; }
			private set
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
			private set
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
			get
			{
				return m_End;
			}

			private set
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

		IEnumerable<IParticipant> IConference.GetParticipants()
		{
			return GetParticipants().Cast<IParticipant>();
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
			participant.OnStartTimeChanged += ParticipantOnOnStartTimeChanged;
			participant.OnEndTimeChanged += ParticipantOnOnEndTimeChanged;
		}

		/// <summary>
		/// Unsubscribes from the participant events.
		/// </summary>
		/// <param name="participant"></param>
		private void Unsubscribe(IParticipant participant)
		{
			participant.OnStatusChanged -= ParticipantOnStatusChanged;
			participant.OnStartTimeChanged -= ParticipantOnOnStartTimeChanged;
			participant.OnEndTimeChanged -= ParticipantOnOnEndTimeChanged;
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

		private void ParticipantOnOnStartTimeChanged(object sender, DateTimeNullableEventArgs e)
		{
			UpdateStartTime();
		}

		private void ParticipantOnOnEndTimeChanged(object sender, DateTimeNullableEventArgs e)
		{
			UpdateEndTime();
		}

		#endregion

		#region Console

		public virtual string ConsoleName { get { return GetType().Name; } }

		public virtual string ConsoleHelp { get { return string.Empty; }  }

		public virtual IEnumerable<IConsoleNodeBase> GetConsoleNodes()
		{
			yield return ConsoleNodeGroup.IndexNodeMap("Participants", "The collection of participants in this conference", GetParticipants());
		}

		public virtual void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			addRow("Status", Status);
			addRow("ParticipantCount", GetParticipants().Count());
		}

		public virtual IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			yield break;
		}

		#endregion
	}
}