using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public sealed class TraditionalConference : AbstractConference<ITraditionalParticipant>, ITraditionalConference, IDisposable
	{
		/// <summary>
		/// Called when the conference status changes.
		/// </summary>
		public override event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;

		/// <summary>
		/// Called when a participant is added to the conference.
		/// </summary>
		public override event EventHandler<ParticipantEventArgs> OnParticipantAdded;

		/// <summary>
		/// Called when a participant is removed from the conference.
		/// </summary>
		public override event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		private readonly IcdHashSet<ITraditionalParticipant> m_Participants;
		private readonly SafeCriticalSection m_ParticipantsSection;

		private eConferenceStatus m_Status;

		/// <summary>
		/// Maps participant status to conference status.
		/// </summary>
		private static readonly Dictionary<eParticipantStatus, eConferenceStatus> s_StatusMap =
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

		/// <summary>
		/// Current conference status.
		/// </summary>
		public override eConferenceStatus Status { get { return m_Status; } }

		/// <summary>
		/// The time the conference started.
		/// </summary>
		public override DateTime? Start
		{
			get
			{
				DateTime start;
				if (m_Participants.Select(s => s.Start)
				             .OfType<DateTime>()
				             .Order()
				             .TryFirst(out start))
					return start;

				return null;
			}
		}

		/// <summary>
		/// The time the conference ended.
		/// </summary>
		public override DateTime? End
		{
			get
			{
				DateTime?[] ends = m_Participants.Select(s => s.End).ToArray();

				// Conference hasn't ended yet.
				if (ends.Length == 0 || ends.Any(e => e == null))
					return null;

				return ends.ExceptNulls().Max();
			}
		}

		public override eCallType CallType
		{
			get { return this.GetOnlineParticipants().Max(p => p.SourceType); }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Constructor.
		/// </summary>
		public TraditionalConference()
		{
			m_Participants = new IcdHashSet<ITraditionalParticipant>();
			m_ParticipantsSection = new SafeCriticalSection();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Clears the sources from the conference.
		/// </summary>
		public void Clear()
		{
			foreach (ITraditionalParticipant participant in GetParticipants())
				RemoveParticipant(participant);
		}

		/// <summary>
		/// Adds the participant to the conference.
		/// </summary>
		/// <param name="participant"></param>
		/// <returns>False if the participant is already in the conference.</returns>
		public bool AddParticipant(ITraditionalParticipant participant)
		{
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
		public bool RemoveParticipant(ITraditionalParticipant participant)
		{
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
		public override IEnumerable<ITraditionalParticipant> GetParticipants()
		{
			return m_ParticipantsSection.Execute(() => m_Participants.ToArray());
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

		/// <summary>
		/// Subscribes to the participant events.
		/// </summary>
		/// <param name="source"></param>
		private void Subscribe(IParticipant source)
		{
			if (source == null)
				return;

			source.OnStatusChanged += SourceOnStatusChanged;
		}

		/// <summary>
		/// Unsubscribes from the participant events.
		/// </summary>
		/// <param name="source"></param>
		private void Unsubscribe(IParticipant source)
		{
			if (source == null)
				return;

			source.OnStatusChanged -= SourceOnStatusChanged;
		}

		/// <summary>
		/// Called when a participant status changes.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void SourceOnStatusChanged(object sender, ParticipantStatusEventArgs args)
		{
			UpdateStatus();
		}

		private void UpdateStatus()
		{
			eConferenceStatus status = GetStatusFromSources();
			if (status == m_Status)
				return;

			m_Status = status;

			OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(m_Status));
		}

		private eConferenceStatus GetStatusFromSources()
		{
			IEnumerable<eConferenceStatus> enumerable = GetParticipants().Select(s => s_StatusMap[s.Status]);
			IcdHashSet<eConferenceStatus> statuses = new IcdHashSet<eConferenceStatus>(enumerable);

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

		#endregion
	}
}
