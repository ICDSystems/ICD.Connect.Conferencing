using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.EventArguments;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.EventArguments;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.Conferences
{
	public abstract class AbstractConference<T> : AbstractConferenceBase<T>
		where T: class, IParticipant
	{
		#region Events

		/// <summary>
		/// Raised when a participant is added to the conference.
		/// </summary>
		public override event EventHandler<ParticipantEventArgs> OnParticipantAdded;

		/// <summary>
		/// Raised when a participant is removed from the conference.
		/// </summary>
		public override event EventHandler<ParticipantEventArgs> OnParticipantRemoved;

		#endregion

		#region Members

		private readonly IcdHashSet<T> m_Participants;
		private readonly SafeCriticalSection m_ParticipantsSection;

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
				{eParticipantStatus.Alerting, eConferenceStatus.Connecting},
				{eParticipantStatus.Connecting, eConferenceStatus.Connecting},
				{eParticipantStatus.Observer, eConferenceStatus.Connected},
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

		#endregion

		#region Constructor

		/// <summary>
		/// Constructor.
		/// </summary>
		protected AbstractConference()
		{
			m_Participants = new IcdHashSet<T>();
			m_ParticipantsSection = new SafeCriticalSection();
		}

		#endregion

		#region Methods

		/// <summary>
		/// Clears the sources from the conference.
		/// </summary>
		protected void ClearParticipants()
		{
			foreach (T participant in GetParticipants())
				RemoveParticipant(participant);
		}

		/// <summary>
		/// Adds the participant to the conference.
		/// </summary>
		/// <param name="participant"></param>
		/// <returns>False if the participant is already in the conference.</returns>
		protected bool AddParticipant([NotNull] T participant)
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
		protected bool RemoveParticipant([NotNull] T participant)
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
		public override IEnumerable<T> GetParticipants()
		{
			return m_ParticipantsSection.Execute(() => m_Participants.ToArray());
		}

		#endregion

		#region Private Methods

		private void UpdateStatus()
		{
			Status = GetStatusFromParticipants();
			CallType = GetCallTypeFromParticipants();
			UpdateStartAndEndTime();
		}

		private eCallType GetCallTypeFromParticipants()
		{
			eCallType callType = eCallType.Unknown;

			foreach (var participant in GetParticipants())
				callType = callType.IncludeFlags(participant.CallType);

			return callType;
		}

		private eConferenceStatus GetStatusFromParticipants()
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
			
		}

		#endregion
	}
}