using System;
using System.Collections.Generic;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Conferences;
using ICD.Connect.Conferencing.IncomingCalls;
using ICD.Connect.Conferencing.Participants;

namespace ICD.Connect.Conferencing.ConferenceManagers.History
{
	public sealed class HistoricalIncomingConference : IHistoricalConference
	{
		private readonly HistoricalIncomingParticipant m_Participant;
		private eConferenceStatus m_Status;

		#region Properties

		public eCallAnswerState AnswerState { get { return m_Participant.AnswerState; } }

		/// <summary>
		/// Raised when the conference status changes.
		/// </summary>
		public event EventHandler<ConferenceStatusEventArgs> OnStatusChanged;
		public DateTime? StartTime { get; private set; }
		public DateTime? EndTime { get; private set; }

		/// <summary>
		/// Gets the status of the conference
		/// </summary>
		public eConferenceStatus Status
		{
			get { return m_Status; }
			private set
			{
				if (m_Status == value)
					return;

				m_Status = value;

				OnStatusChanged.Raise(this, new ConferenceStatusEventArgs(value));
			}
		}

		#endregion

		public IEnumerable<IHistoricalParticipant> GetParticipants()
		{
			yield return m_Participant;
		}

		/// <summary>
		/// Detach HistoricalConference from the underlying conference/incoming call
		/// This is called when the conference gets removed, to unsubscribe
		/// and remove references to the conference
		/// </summary>
		public void Detach()
		{
			m_Participant.Detach();
		}

		public HistoricalIncomingConference(IIncomingCall incomingCall)
		{
			m_Participant = new HistoricalIncomingParticipant(incomingCall);
		}
	}
}
